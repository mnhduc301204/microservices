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
        if (!await dbContext.TryBeginProcessingAsync(message.EventId, ConsumerName, context.CancellationToken))
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

        await using var transaction = await dbContext.Database.BeginTransactionAsync(context.CancellationToken);

        foreach (var line in normalizedLines)
        {
            var reserved = await dbContext.Items
                .Where(item => item.Sku == line.Sku && item.QuantityOnHand - item.QuantityReserved >= line.Quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.QuantityReserved, item => item.QuantityReserved + line.Quantity), context.CancellationToken);

            if (reserved == 0)
            {
                await transaction.RollbackAsync(context.CancellationToken);
                dbContext.ChangeTracker.Clear();

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
        await transaction.CommitAsync(context.CancellationToken);
    }
}
