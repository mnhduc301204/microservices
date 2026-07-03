namespace ECommerce.Ordering.Models;

public sealed class OrderItem
{
    private OrderItem()
    {
    }

    public OrderItem(Guid orderId, string sku, string productName, decimal unitPrice, int quantity)
    {
        OrderId = orderId;
        Sku = string.IsNullOrWhiteSpace(sku) ? throw new ArgumentException("SKU is required.", nameof(sku)) : sku.Trim().ToUpperInvariant();
        ProductName = string.IsNullOrWhiteSpace(productName) ? throw new ArgumentException("Product name is required.", nameof(productName)) : productName.Trim();
        UnitPrice = unitPrice < 0 ? throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.") : unitPrice;
        Quantity = quantity <= 0 ? throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.") : quantity;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid OrderId { get; private set; }

    public string Sku { get; private set; } = string.Empty;

    public string ProductName { get; private set; } = string.Empty;

    public decimal UnitPrice { get; private set; }

    public int Quantity { get; private set; }
}
