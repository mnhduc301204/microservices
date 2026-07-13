using ECommerce.Contracts;
using ECommerce.Contracts.Payment;
using ECommerce.Payment.Data;
using ECommerce.Payment.Models;
using ECommerce.ServiceDefaults.Messaging;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Payment.BackgroundServices;

public sealed class PaymentOutboxRepairService(
    IServiceProvider serviceProvider,
    ILogger<PaymentOutboxRepairService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RepairBatch(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to repair missing payment outbox messages.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task RepairBatch(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

        var payments = await dbContext.Payments
            .Where(payment => payment.CustomerId != Guid.Empty
                && (payment.Status == PaymentStatus.Succeeded || payment.Status == PaymentStatus.Failed || payment.Status == PaymentStatus.Refunded))
            .Where(payment => !dbContext.Set<OutboxMessage>().Any(message =>
                message.Topic == (payment.Status == PaymentStatus.Succeeded
                    ? KafkaTopics.PaymentSucceeded
                    : payment.Status == PaymentStatus.Failed
                        ? KafkaTopics.PaymentFailed
                        : KafkaTopics.PaymentRefunded)
                && message.Payload.Contains(payment.Id.ToString())))
            .OrderBy(payment => payment.CompletedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var payment in payments)
        {
            var paymentId = payment.Id.ToString();
            if (payment.Status == PaymentStatus.Succeeded)
            {
                if (!await HasPaymentOutbox(dbContext, KafkaTopics.PaymentSucceeded, paymentId, cancellationToken))
                {
                    dbContext.Set<OutboxMessage>().Add(OutboxMessage.Create(
                        KafkaTopics.PaymentSucceeded,
                        new PaymentSucceededIntegrationEvent(
                            Guid.NewGuid(),
                            DateTimeOffset.UtcNow,
                            payment.Id,
                            payment.OrderId,
                            payment.CustomerId,
                            payment.CustomerEmail,
                            payment.Amount)));
                }
            }
            else if (payment.Status == PaymentStatus.Failed)
            {
                if (!await HasPaymentOutbox(dbContext, KafkaTopics.PaymentFailed, paymentId, cancellationToken))
                {
                    var reason = payment.FailureReason ?? "Payment failed.";
                    dbContext.Set<OutboxMessage>().Add(OutboxMessage.Create(
                        KafkaTopics.PaymentFailed,
                        new PaymentFailedIntegrationEvent(
                            Guid.NewGuid(),
                            DateTimeOffset.UtcNow,
                            payment.Id,
                            payment.OrderId,
                            payment.CustomerId,
                            reason)));
                }
            }
            else if (payment.Status == PaymentStatus.Refunded)
            {
                if (!await HasPaymentOutbox(dbContext, KafkaTopics.PaymentRefunded, paymentId, cancellationToken))
                {
                    dbContext.Set<OutboxMessage>().Add(OutboxMessage.Create(
                        KafkaTopics.PaymentRefunded,
                        new PaymentRefundedIntegrationEvent(
                            Guid.NewGuid(),
                            DateTimeOffset.UtcNow,
                            payment.Id,
                            payment.OrderId,
                            payment.CustomerId,
                            payment.Amount,
                            "Payment refund was repaired from local payment state.")));
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Task<bool> HasPaymentOutbox(
        PaymentDbContext dbContext,
        string topic,
        string paymentId,
        CancellationToken cancellationToken)
    {
        return dbContext.Set<OutboxMessage>()
            .AnyAsync(message => message.Topic == topic && message.Payload.Contains(paymentId), cancellationToken);
    }
}
