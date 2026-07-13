using ECommerce.Contracts.Ordering;
using ECommerce.Inventory.Data;
using ECommerce.Inventory.Models;
using ECommerce.ServiceDefaults.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Inventory.Consumers;

public sealed class OrderConfirmedConsumer(InventoryDbContext dbContext) : IConsumer<OrderConfirmedIntegrationEvent>
{
    private const string ConsumerName = nameof(OrderConfirmedConsumer);

    public async Task Consume(ConsumeContext<OrderConfirmedIntegrationEvent> context)
    {
        var message = context.Message;
        if (!await dbContext.TryBeginProcessingAsync(message.EventId, ConsumerName, context.CancellationToken))
        {
            return;
        }

        var reservations = await dbContext.Reservations
            .AsNoTracking()
            .Where(reservation => reservation.OrderId == message.OrderId && reservation.Status == StockReservationStatus.Reserved)
            .ToListAsync(context.CancellationToken);

        foreach (var reservation in reservations)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(context.CancellationToken);
            var claimed = await dbContext.Reservations
                .Where(candidate => candidate.Id == reservation.Id && candidate.Status == StockReservationStatus.Reserved)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(candidate => candidate.Status, StockReservationStatus.Deducted)
                    .SetProperty(candidate => candidate.CompletedAt, DateTimeOffset.UtcNow), context.CancellationToken);

            if (claimed == 0)
            {
                await transaction.RollbackAsync(context.CancellationToken);
                continue;
            }

            var item = await dbContext.Items.FirstAsync(item => item.Sku == reservation.Sku, context.CancellationToken);
            item.DeductReserved(reservation.Quantity);
            dbContext.StockMovements.Add(new StockMovement(
                reservation.Sku,
                reservation.Quantity,
                StockMovementType.Deducted,
                reservation.ReservationId,
                reservation.OrderId,
                "Order confirmed stock deduction."));

            await dbContext.SaveChangesAsync(context.CancellationToken);
            await transaction.CommitAsync(context.CancellationToken);
        }

        dbContext.MarkProcessed(message.EventId, ConsumerName);
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
