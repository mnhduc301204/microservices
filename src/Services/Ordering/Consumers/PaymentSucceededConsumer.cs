using ECommerce.Contracts;
using ECommerce.Contracts.Ordering;
using ECommerce.Contracts.Payment;
using ECommerce.Ordering.Data;
using ECommerce.ServiceDefaults.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Consumers;

public sealed class PaymentSucceededConsumer(OrderingDbContext dbContext) : IConsumer<PaymentSucceededIntegrationEvent>
{
    private const string ConsumerName = nameof(PaymentSucceededConsumer);

    public async Task Consume(ConsumeContext<PaymentSucceededIntegrationEvent> context)
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

        order.ConfirmPayment();
        var saga = await dbContext.CheckoutSagas.FirstOrDefaultAsync(saga => saga.OrderId == message.OrderId, context.CancellationToken);
        saga?.MarkPaymentSucceeded(message.PaymentId);

        dbContext.Set<OutboxMessage>().Add(OutboxMessage.Create(
            KafkaTopics.OrderConfirmed,
            new OrderConfirmedIntegrationEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, order.Id, order.CustomerId, order.Total)));
        dbContext.MarkProcessed(message.EventId, ConsumerName);
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
