using ECommerce.Contracts.Ordering;
using ECommerce.Notification.Data;
using MassTransit;

namespace ECommerce.Notification.Consumers;

public sealed class OrderCreatedNotificationConsumer(NotificationDbContext dbContext)
    : NotificationConsumerBase(dbContext), IConsumer<OrderCreatedIntegrationEvent>
{
    public Task Consume(ConsumeContext<OrderCreatedIntegrationEvent> context)
    {
        var message = context.Message;
        return RecordOnce(
            message.EventId,
            nameof(OrderCreatedIntegrationEvent),
            message.CustomerEmail,
            $"Order {message.OrderId} was created.",
            context.CancellationToken);
    }
}
