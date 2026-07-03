using ECommerce.Contracts.Payment;
using ECommerce.Notification.Data;
using MassTransit;

namespace ECommerce.Notification.Consumers;

public sealed class PaymentSucceededNotificationConsumer(NotificationDbContext dbContext)
    : NotificationConsumerBase(dbContext), IConsumer<PaymentSucceededIntegrationEvent>
{
    public Task Consume(ConsumeContext<PaymentSucceededIntegrationEvent> context)
    {
        var message = context.Message;
        return RecordOnce(
            message.EventId,
            nameof(PaymentSucceededIntegrationEvent),
            message.CustomerEmail,
            $"Payment {message.PaymentId} succeeded for order {message.OrderId}.",
            context.CancellationToken);
    }
}
