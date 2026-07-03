namespace ECommerce.Notification.Features.RecordNotification;

public sealed record RecordNotificationCommand(string Type, string Recipient, string Message, Guid? SourceEventId);
