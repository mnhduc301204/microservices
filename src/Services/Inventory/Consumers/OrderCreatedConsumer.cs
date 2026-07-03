using ECommerce.Contracts;
using ECommerce.Contracts.Inventory;
using ECommerce.Contracts.Ordering;
using ECommerce.Inventory.Data;
using ECommerce.Inventory.Models;
using ECommerce.ServiceDefaults.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Inventory.Consumers;

public sealed class OrderCreatedConsumer(InventoryDbContext dbContext) : IConsumer<OrderCreatedIntegrationEvent>
{
    private const string ConsumerName = nameof(OrderCreatedConsumer);

    public async Task Consume(ConsumeContext<OrderCreatedIntegrationEvent> context)
    {
        var message = context.Message;
        if (await dbContext.HasProcessedAsync(message.EventId, ConsumerName, context.CancellationToken))
        {
            return;
        }

        var reservationId = Guid.NewGuid();
        var normalizedLines = message.Lines
            .GroupBy(line => InventoryItem.NormalizeSku(line.Sku))
            .Select(group => new
            {
                Sku = group.Key,
                Quantity = group.Sum(line => line.Quantity),
            })
            .ToArray();

        foreach (var line in normalizedLines)
        {
            var item = await dbContext.Items.FirstOrDefaultAsync(item => item.Sku == line.Sku, context.CancellationToken);
            if (item is null || item.AvailableQuantity < line.Quantity)
            {
                dbContext.Set<OutboxMessage>().Add(OutboxMessage.Create(
                    KafkaTopics.StockReservationFailed,
                    new StockReservationFailedIntegrationEvent(
                        Guid.NewGuid(),
                        DateTimeOffset.UtcNow,
                        message.OrderId,
                        message.CustomerId,
                        $"Insufficient stock for {line.Sku}.")));
                dbContext.MarkProcessed(message.EventId, ConsumerName);
                await dbContext.SaveChangesAsync(context.CancellationToken);
                return;
            }
        }

        foreach (var line in normalizedLines)
        {
            var item = await dbContext.Items.FirstAsync(item => item.Sku == line.Sku, context.CancellationToken);
            item.TryReserve(line.Quantity);
            dbContext.Reservations.Add(new StockReservation(reservationId, line.Sku, line.Quantity, message.OrderId));
            dbContext.StockMovements.Add(new StockMovement(
                line.Sku,
                line.Quantity,
                StockMovementType.Reserved,
                reservationId,
                message.OrderId,
                "Order created stock reservation."));
        }

        dbContext.Set<OutboxMessage>().Add(OutboxMessage.Create(
            KafkaTopics.StockReserved,
            new StockReservedIntegrationEvent(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                reservationId,
                message.OrderId,
                message.CustomerId,
                message.CustomerEmail,
                message.Total,
                normalizedLines.Select(line => new StockReservedLine(line.Sku, line.Quantity)).ToArray())));
        dbContext.MarkProcessed(message.EventId, ConsumerName);
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
