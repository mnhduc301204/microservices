namespace ECommerce.Contracts.Inventory;

public sealed record StockReservationFailedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    Guid CustomerId,
    string Reason);
