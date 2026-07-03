namespace ECommerce.Notification.IntegrationEvents;

public sealed record NotificationRecordedIntegrationEvent(Guid EventId, DateTimeOffset OccurredAt, Guid NotificationId, string Type);
