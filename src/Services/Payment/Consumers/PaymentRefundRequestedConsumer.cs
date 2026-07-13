using ECommerce.Contracts;
using ECommerce.Contracts.Payment;
using ECommerce.Payment.Data;
using ECommerce.Payment.Models;
using ECommerce.ServiceDefaults.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Payment.Consumers;

public sealed class PaymentRefundRequestedConsumer(PaymentDbContext dbContext) : IConsumer<PaymentRefundRequestedIntegrationEvent>
{
    private const string ConsumerName = nameof(PaymentRefundRequestedConsumer);

    public async Task Consume(ConsumeContext<PaymentRefundRequestedIntegrationEvent> context)
    {
        var message = context.Message;
        if (!await dbContext.TryBeginProcessingAsync(message.EventId, ConsumerName, context.CancellationToken))
        {
            return;
        }

        var payment = await dbContext.Payments.FirstOrDefaultAsync(
            payment => payment.Id == message.PaymentId && payment.OrderId == message.OrderId,
            context.CancellationToken);

        if (payment is null)
        {
            dbContext.MarkProcessed(message.EventId, ConsumerName);
            await dbContext.SaveChangesAsync(context.CancellationToken);
            return;
        }

        if (payment.Status == PaymentStatus.Succeeded)
        {
            payment.Refund();
            dbContext.Set<OutboxMessage>().Add(OutboxMessage.Create(
                KafkaTopics.PaymentRefunded,
                new PaymentRefundedIntegrationEvent(
                    Guid.NewGuid(),
                    DateTimeOffset.UtcNow,
                    payment.Id,
                    payment.OrderId,
                    message.CustomerId,
                    payment.Amount,
                    message.Reason)));
        }

        dbContext.MarkProcessed(message.EventId, ConsumerName);
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
