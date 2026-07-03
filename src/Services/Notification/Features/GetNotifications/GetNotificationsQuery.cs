namespace ECommerce.Notification.Features.GetNotifications;

public sealed record GetNotificationsQuery(int PageNumber = 1, int PageSize = 50);
