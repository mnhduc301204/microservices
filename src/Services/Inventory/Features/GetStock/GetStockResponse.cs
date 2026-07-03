namespace ECommerce.Inventory.Features.GetStock;

public sealed record GetStockResponse(string Sku, int QuantityOnHand, int QuantityReserved, int AvailableQuantity);
