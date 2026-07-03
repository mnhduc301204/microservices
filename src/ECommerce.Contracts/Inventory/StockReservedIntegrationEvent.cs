namespace ECommerce.Contracts.Inventory;

public sealed record StockReservedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid ReservationId,
    Guid OrderId,
    Guid CustomerId,
    string CustomerEmail,
    decimal Total,
    IReadOnlyCollection<StockReservedLine> Lines);

public sealed record StockReservedLine(string Sku, int Quantity);
