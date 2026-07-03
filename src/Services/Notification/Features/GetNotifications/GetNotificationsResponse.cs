using ECommerce.Notification.Contracts;

namespace ECommerce.Notification.Features.GetNotifications;

public sealed record GetNotificationsResponse(IReadOnlyCollection<NotificationDto> Items);
