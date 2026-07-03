namespace ECommerce.Notification.Contracts;

public sealed record NotificationDto(Guid Id, string Type, string Recipient, string Message, Guid? SourceEventId, DateTimeOffset CreatedAt);
