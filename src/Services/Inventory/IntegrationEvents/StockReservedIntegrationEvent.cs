namespace ECommerce.Inventory.IntegrationEvents;

public sealed record StockReservedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid ReservationId,
    Guid OrderId,
    IReadOnlyCollection<StockReservedLine> Lines);

public sealed record StockReservedLine(string Sku, int Quantity);
