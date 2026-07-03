using ECommerce.Contracts.Inventory;
using ECommerce.Inventory.Data;
using ECommerce.Inventory.Models;
using ECommerce.ServiceDefaults.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Inventory.Consumers;

public sealed class ReleaseStockReservationConsumer(InventoryDbContext dbContext) : IConsumer<ReleaseStockReservationIntegrationEvent>
{
    private const string ConsumerName = nameof(ReleaseStockReservationConsumer);

    public async Task Consume(ConsumeContext<ReleaseStockReservationIntegrationEvent> context)
    {
        var message = context.Message;
        if (!await dbContext.TryBeginProcessingAsync(message.EventId, ConsumerName, context.CancellationToken))
        {
            return;
        }

        var reservations = await dbContext.Reservations
            .AsNoTracking()
            .Where(reservation => reservation.ReservationId == message.ReservationId && reservation.Status == StockReservationStatus.Reserved)
            .ToListAsync(context.CancellationToken);

        foreach (var reservation in reservations)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(context.CancellationToken);
            var claimed = await dbContext.Reservations
                .Where(candidate => candidate.Id == reservation.Id && candidate.Status == StockReservationStatus.Reserved)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(candidate => candidate.Status, StockReservationStatus.Released)
                    .SetProperty(candidate => candidate.CompletedAt, DateTimeOffset.UtcNow), context.CancellationToken);

            if (claimed == 0)
            {
                await transaction.RollbackAsync(context.CancellationToken);
                continue;
            }

            var item = await dbContext.Items.FirstAsync(item => item.Sku == reservation.Sku, context.CancellationToken);
            item.Release(reservation.Quantity);
            dbContext.StockMovements.Add(new StockMovement(
                reservation.Sku,
                reservation.Quantity,
                StockMovementType.Released,
                reservation.ReservationId,
                reservation.OrderId,
                message.Reason));
            await dbContext.SaveChangesAsync(context.CancellationToken);
            await transaction.CommitAsync(context.CancellationToken);
        }

        dbContext.MarkProcessed(message.EventId, ConsumerName);
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
