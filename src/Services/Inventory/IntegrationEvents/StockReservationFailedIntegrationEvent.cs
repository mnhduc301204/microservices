namespace ECommerce.Inventory.IntegrationEvents;

public sealed record StockReservationFailedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    string Reason);
