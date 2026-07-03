using ECommerce.Contracts;
using ECommerce.Contracts.Inventory;
using ECommerce.Contracts.Ordering;
using ECommerce.Contracts.Payment;
using ECommerce.Ordering.Data;
using ECommerce.ServiceDefaults.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Consumers;

public sealed class PaymentFailedConsumer(OrderingDbContext dbContext) : IConsumer<PaymentFailedIntegrationEvent>
{
    private const string ConsumerName = nameof(PaymentFailedConsumer);

    public async Task Consume(ConsumeContext<PaymentFailedIntegrationEvent> context)
    {
        var message = context.Message;
        if (!await dbContext.TryBeginProcessingAsync(message.EventId, ConsumerName, context.CancellationToken))
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

        if (saga?.StockReservationId is Guid reservationId)
        {
            dbContext.Set<OutboxMessage>().Add(OutboxMessage.Create(
                KafkaTopics.ReleaseStockReservation,
                new ReleaseStockReservationIntegrationEvent(
                    Guid.NewGuid(),
                    DateTimeOffset.UtcNow,
                    reservationId,
                    order.Id,
                    message.Reason)));
        }

        dbContext.MarkProcessed(message.EventId, ConsumerName);
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
