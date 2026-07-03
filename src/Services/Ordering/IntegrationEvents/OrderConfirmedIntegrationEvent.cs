namespace ECommerce.Ordering.IntegrationEvents;

public sealed record OrderConfirmedIntegrationEvent(Guid EventId, DateTimeOffset OccurredAt, Guid OrderId);
