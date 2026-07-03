using ECommerce.Contracts;
using ECommerce.Contracts.Inventory;
using ECommerce.Contracts.Ordering;
using ECommerce.Ordering.Data;
using ECommerce.Ordering.Models;
using ECommerce.ServiceDefaults.Messaging;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.BackgroundServices;

public sealed class CheckoutSagaTimeoutService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<CheckoutSagaTimeoutService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CancelTimedOutSagas(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to cancel timed out checkout sagas.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task CancelTimedOutSagas(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var timeout = TimeSpan.FromMinutes(configuration.GetValue("CheckoutSaga:TimeoutMinutes", 15));
        var cutoff = DateTimeOffset.UtcNow.Subtract(timeout);

        var sagaIds = await dbContext.CheckoutSagas
            .Where(saga =>
                saga.Status != CheckoutSagaStatus.Completed
                && saga.Status != CheckoutSagaStatus.Failed
                && saga.UpdatedAt <= cutoff)
            .OrderBy(saga => saga.UpdatedAt)
            .Take(50)
            .Select(saga => saga.CheckoutId)
            .ToListAsync(cancellationToken);

        foreach (var sagaId in sagaIds)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var saga = await dbContext.CheckoutSagas.FirstOrDefaultAsync(
                candidate => candidate.CheckoutId == sagaId
                    && candidate.Status != CheckoutSagaStatus.Completed
                    && candidate.Status != CheckoutSagaStatus.Failed,
                cancellationToken);

            if (saga is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                continue;
            }

            var order = await dbContext.Orders.FirstOrDefaultAsync(order => order.Id == saga.OrderId, cancellationToken);
            if (order is null)
            {
                saga.MarkFailed("Checkout timed out and order was not found.");
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                continue;
            }

            var reason = "Checkout timed out.";
            order.Fail();
            saga.MarkFailed(reason);
            dbContext.Set<OutboxMessage>().Add(OutboxMessage.Create(
                KafkaTopics.OrderCancelled,
                new OrderCancelledIntegrationEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, order.Id, order.CustomerId, reason)));

            if (saga.StockReservationId is Guid reservationId)
            {
                dbContext.Set<OutboxMessage>().Add(OutboxMessage.Create(
                    KafkaTopics.ReleaseStockReservation,
                    new ReleaseStockReservationIntegrationEvent(
                        Guid.NewGuid(),
                        DateTimeOffset.UtcNow,
                        reservationId,
                        order.Id,
                        reason)));
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
    }
}
