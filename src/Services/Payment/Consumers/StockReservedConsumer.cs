using ECommerce.Contracts;
using ECommerce.Contracts.Inventory;
using ECommerce.Contracts.Payment;
using ECommerce.Payment.Data;
using ECommerce.ServiceDefaults.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Payment.Consumers;

public sealed class StockReservedConsumer(PaymentDbContext dbContext) : IConsumer<StockReservedIntegrationEvent>
{
    private const string ConsumerName = nameof(StockReservedConsumer);

    public async Task Consume(ConsumeContext<StockReservedIntegrationEvent> context)
    {
        var message = context.Message;
        if (await dbContext.HasProcessedAsync(message.EventId, ConsumerName, context.CancellationToken))
        {
            return;
        }

        var payment = await dbContext.Payments.FirstOrDefaultAsync(payment => payment.OrderId == message.OrderId, context.CancellationToken);
        if (payment is null)
        {
            payment = new Models.Payment(message.OrderId, message.Total, "USD");
            dbContext.Payments.Add(payment);
        }

        try
        {
            payment.MarkSucceeded();
            dbContext.Set<OutboxMessage>().Add(OutboxMessage.Create(
                KafkaTopics.PaymentSucceeded,
                new PaymentSucceededIntegrationEvent(
                    Guid.NewGuid(),
                    DateTimeOffset.UtcNow,
                    payment.Id,
                    payment.OrderId,
                    message.CustomerId,
                    message.CustomerEmail,
                    payment.Amount)));
        }
        catch (InvalidOperationException ex)
        {
            dbContext.Set<OutboxMessage>().Add(OutboxMessage.Create(
                KafkaTopics.PaymentFailed,
                new PaymentFailedIntegrationEvent(
                    Guid.NewGuid(),
                    DateTimeOffset.UtcNow,
                    payment.Id,
                    payment.OrderId,
                    message.CustomerId,
                    ex.Message)));
        }

        dbContext.MarkProcessed(message.EventId, ConsumerName);
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
