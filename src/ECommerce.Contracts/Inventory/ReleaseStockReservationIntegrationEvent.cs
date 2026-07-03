namespace ECommerce.Contracts.Inventory;

public sealed record ReleaseStockReservationIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid ReservationId,
    Guid OrderId,
    string Reason);
