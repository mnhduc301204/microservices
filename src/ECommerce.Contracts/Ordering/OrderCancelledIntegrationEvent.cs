namespace ECommerce.Contracts.Ordering;

public sealed record OrderCancelledIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    Guid CustomerId,
    string Reason);
