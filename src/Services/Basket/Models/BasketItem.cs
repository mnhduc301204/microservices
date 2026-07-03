namespace ECommerce.Basket.Models;

public sealed class BasketItem
{
    private BasketItem()
    {
    }

    public BasketItem(Guid customerId, string sku, string productName, decimal unitPrice, int quantity)
    {
        CustomerId = customerId;
        Sku = NormalizeSku(sku);
        ProductName = string.IsNullOrWhiteSpace(productName) ? throw new ArgumentException("Product name is required.", nameof(productName)) : productName.Trim();
        UnitPrice = unitPrice < 0 ? throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.") : unitPrice;
        ChangeQuantity(quantity);
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid CustomerId { get; private set; }

    public string Sku { get; private set; } = string.Empty;

    public string ProductName { get; private set; } = string.Empty;

    public decimal UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    public void ChangeQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        Quantity = quantity;
    }

    public static string NormalizeSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ArgumentException("SKU is required.", nameof(sku));
        }

        return sku.Trim().ToUpperInvariant();
    }
}
