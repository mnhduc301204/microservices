using ECommerce.Contracts.Ordering;
using ECommerce.Notification.Data;
using MassTransit;

namespace ECommerce.Notification.Consumers;

public sealed class OrderConfirmedNotificationConsumer(NotificationDbContext dbContext)
    : NotificationConsumerBase(dbContext), IConsumer<OrderConfirmedIntegrationEvent>
{
    public Task Consume(ConsumeContext<OrderConfirmedIntegrationEvent> context)
    {
        var message = context.Message;
        return RecordOnce(
            message.EventId,
            nameof(OrderConfirmedIntegrationEvent),
            message.CustomerId.ToString(),
            $"Order {message.OrderId} was confirmed.",
            context.CancellationToken);
    }
}
