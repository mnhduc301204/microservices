using ECommerce.Notification.Data;

namespace ECommerce.Notification.Features.GetNotifications;

public static class GetNotificationsEndpoint
{
    public static RouteGroupBuilder MapGetNotifications(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (NotificationDbContext dbContext, CancellationToken cancellationToken, int pageNumber = 1, int pageSize = 50) =>
            await new GetNotificationsHandler(dbContext).Handle(new GetNotificationsQuery(pageNumber, pageSize), cancellationToken));

        return group;
    }
}
