namespace ECommerce.Contracts.Ordering;

public sealed record OrderCreatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    Guid CustomerId,
    string CustomerEmail,
    decimal Total,
    IReadOnlyCollection<OrderCreatedLine> Lines);

public sealed record OrderCreatedLine(string Sku, string ProductName, decimal UnitPrice, int Quantity);
