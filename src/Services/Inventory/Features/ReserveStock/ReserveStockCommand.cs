namespace ECommerce.Inventory.Features.ReserveStock;

public sealed record ReserveStockCommand(Guid ReservationId, Guid? OrderId, IReadOnlyCollection<ReserveStockLine> Lines);

public sealed record ReserveStockLine(string Sku, int Quantity);
