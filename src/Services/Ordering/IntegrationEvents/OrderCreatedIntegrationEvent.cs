namespace ECommerce.Ordering.IntegrationEvents;

public sealed record OrderCreatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    Guid CustomerId,
    IReadOnlyCollection<OrderCreatedLine> Lines);

public sealed record OrderCreatedLine(string Sku, int Quantity);
