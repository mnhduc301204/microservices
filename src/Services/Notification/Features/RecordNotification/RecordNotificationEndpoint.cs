using ECommerce.Notification.Data;

namespace ECommerce.Notification.Features.RecordNotification;

public static class RecordNotificationEndpoint
{
    public static RouteGroupBuilder MapRecordNotification(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (RecordNotificationCommand command, NotificationDbContext dbContext, CancellationToken cancellationToken) =>
            await new RecordNotificationHandler(dbContext).Handle(command, cancellationToken));

        return group;
    }
}
