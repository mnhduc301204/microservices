namespace ECommerce.Contracts.Ordering;

public sealed record OrderConfirmedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    Guid CustomerId,
    decimal Total);
