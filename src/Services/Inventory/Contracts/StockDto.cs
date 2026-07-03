namespace ECommerce.Inventory.Contracts;

public sealed record StockDto(string Sku, int QuantityOnHand, int QuantityReserved, int AvailableQuantity);
