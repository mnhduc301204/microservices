using ECommerce.Contracts.Ordering;
using ECommerce.Notification.Data;
using MassTransit;

namespace ECommerce.Notification.Consumers;

public sealed class OrderCancelledNotificationConsumer(NotificationDbContext dbContext)
    : NotificationConsumerBase(dbContext), IConsumer<OrderCancelledIntegrationEvent>
{
    public Task Consume(ConsumeContext<OrderCancelledIntegrationEvent> context)
    {
        var message = context.Message;
        return RecordOnce(
            message.EventId,
            nameof(OrderCancelledIntegrationEvent),
            message.CustomerId.ToString(),
            $"Order {message.OrderId} was cancelled: {message.Reason}",
            context.CancellationToken);
    }
}
