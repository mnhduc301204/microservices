using ECommerce.Notification.Data;
using ECommerce.Notification.Models;
using ECommerce.ServiceDefaults.Messaging;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Notification.Consumers;

public abstract class NotificationConsumerBase(NotificationDbContext dbContext)
{
    protected async Task RecordOnce(
        Guid eventId,
        string type,
        string recipient,
        string message,
        CancellationToken cancellationToken)
    {
        if (await dbContext.HasProcessedAsync(eventId, type, cancellationToken))
        {
            return;
        }

        dbContext.Notifications.Add(new NotificationRecord(type, recipient, message, eventId));
        dbContext.MarkProcessed(eventId, type);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
