namespace ECommerce.Inventory.Features.ReserveStock;

public sealed record ReserveStockResponse(Guid ReservationId, bool Reserved, string? Reason);
