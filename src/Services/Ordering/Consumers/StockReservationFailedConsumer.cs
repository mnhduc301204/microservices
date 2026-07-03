using ECommerce.Contracts;
using ECommerce.Contracts.Inventory;
using ECommerce.Contracts.Ordering;
using ECommerce.Ordering.Data;
using ECommerce.ServiceDefaults.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Consumers;

public sealed class StockReservationFailedConsumer(OrderingDbContext dbContext) : IConsumer<StockReservationFailedIntegrationEvent>
{
    private const string ConsumerName = nameof(StockReservationFailedConsumer);

    public async Task Consume(ConsumeContext<StockReservationFailedIntegrationEvent> context)
    {
        var message = context.Message;
        if (await dbContext.HasProcessedAsync(message.EventId, ConsumerName, context.CancellationToken))
        {
            return;
        }

        var order = await dbContext.Orders.FirstOrDefaultAsync(order => order.Id == message.OrderId, context.CancellationToken);
        if (order is null)
        {
            return;
        }

        order.Fail();
        var saga = await dbContext.CheckoutSagas.FirstOrDefaultAsync(saga => saga.OrderId == message.OrderId, context.CancellationToken);
        saga?.MarkFailed(message.Reason);

        dbContext.Set<OutboxMessage>().Add(OutboxMessage.Create(
            KafkaTopics.OrderCancelled,
            new OrderCancelledIntegrationEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, order.Id, order.CustomerId, message.Reason)));
        dbContext.MarkProcessed(message.EventId, ConsumerName);
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
