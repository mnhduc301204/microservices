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
            .Where(reservation => reservation.ReservationId == message.ReservationId && reservation.Status == StockReservationStatus.Reserved)
            .ToListAsync(context.CancellationToken);

        foreach (var reservation in reservations)
        {
            var item = await dbContext.Items.FirstAsync(item => item.Sku == reservation.Sku, context.CancellationToken);
            item.Release(reservation.Quantity);
            reservation.MarkReleased();
            dbContext.StockMovements.Add(new StockMovement(
                reservation.Sku,
                reservation.Quantity,
                StockMovementType.Released,
                reservation.ReservationId,
                reservation.OrderId,
                message.Reason));
        }

        dbContext.MarkProcessed(message.EventId, ConsumerName);
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
