using ECommerce.Notification.Contracts;
using ECommerce.Notification.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Notification.Features.GetNotifications;

public sealed class GetNotificationsHandler(NotificationDbContext dbContext)
{
    public async Task<IResult> Handle(GetNotificationsQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var notifications = await dbContext.Notifications.AsNoTracking()
            .OrderByDescending(notification => notification.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(notification => new NotificationDto(
                notification.Id,
                notification.Type,
                notification.Recipient,
                notification.Message,
                notification.SourceEventId,
                notification.CreatedAt))
            .ToListAsync(cancellationToken);

        return Results.Ok(new GetNotificationsResponse(notifications));
    }
}
