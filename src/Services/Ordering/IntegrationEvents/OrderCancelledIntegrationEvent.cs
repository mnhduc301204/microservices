namespace ECommerce.Ordering.IntegrationEvents;

public sealed record OrderCancelledIntegrationEvent(Guid EventId, DateTimeOffset OccurredAt, Guid OrderId);
