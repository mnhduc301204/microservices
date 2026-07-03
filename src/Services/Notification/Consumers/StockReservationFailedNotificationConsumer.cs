using ECommerce.Contracts.Inventory;
using ECommerce.Notification.Data;
using MassTransit;

namespace ECommerce.Notification.Consumers;

public sealed class StockReservationFailedNotificationConsumer(NotificationDbContext dbContext)
    : NotificationConsumerBase(dbContext), IConsumer<StockReservationFailedIntegrationEvent>
{
    public Task Consume(ConsumeContext<StockReservationFailedIntegrationEvent> context)
    {
        var message = context.Message;
        return RecordOnce(
            message.EventId,
            nameof(StockReservationFailedIntegrationEvent),
            message.CustomerId.ToString(),
            $"Stock reservation failed for order {message.OrderId}: {message.Reason}",
            context.CancellationToken);
    }
}
