using ECommerce.Contracts.Payment;
using ECommerce.Notification.Data;
using MassTransit;

namespace ECommerce.Notification.Consumers;

public sealed class PaymentFailedNotificationConsumer(NotificationDbContext dbContext)
    : NotificationConsumerBase(dbContext), IConsumer<PaymentFailedIntegrationEvent>
{
    public Task Consume(ConsumeContext<PaymentFailedIntegrationEvent> context)
    {
        var message = context.Message;
        return RecordOnce(
            message.EventId,
            nameof(PaymentFailedIntegrationEvent),
            message.CustomerId.ToString(),
            $"Payment failed for order {message.OrderId}: {message.Reason}",
            context.CancellationToken);
    }
}
