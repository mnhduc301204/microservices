namespace ECommerce.Inventory.Models;

public sealed class InventoryItem
{
    private InventoryItem()
    {
    }

    public InventoryItem(string sku, int quantityOnHand)
    {
        if (quantityOnHand < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantityOnHand), "Quantity cannot be negative.");
        }

        Sku = NormalizeSku(sku);
        QuantityOnHand = quantityOnHand;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Sku { get; private set; } = string.Empty;

    public int QuantityOnHand { get; private set; }

    public int QuantityReserved { get; private set; }

    public int AvailableQuantity => QuantityOnHand - QuantityReserved;

    public bool TryReserve(int quantity)
    {
        if (quantity <= 0 || AvailableQuantity < quantity)
        {
            return false;
        }

        QuantityReserved += quantity;
        return true;
    }

    public void Release(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Release quantity must be positive.");
        }

        QuantityReserved = Math.Max(0, QuantityReserved - quantity);
    }

    public void DeductReserved(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Deduct quantity must be positive.");
        }

        if (QuantityReserved < quantity)
        {
            throw new InvalidOperationException("Cannot deduct more than reserved quantity.");
        }

        QuantityReserved -= quantity;
        QuantityOnHand -= quantity;
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
