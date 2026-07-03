namespace ECommerce.Inventory.Models;

public sealed class StockMovement
{
    private StockMovement()
    {
    }

    public StockMovement(string sku, int quantity, StockMovementType type, Guid? reservationId, Guid? orderId, string reason)
    {
        Sku = InventoryItem.NormalizeSku(sku);
        Quantity = quantity;
        Type = type;
        ReservationId = reservationId;
        OrderId = orderId;
        Reason = string.IsNullOrWhiteSpace(reason) ? type.ToString() : reason.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Sku { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    public StockMovementType Type { get; private set; }

    public Guid? ReservationId { get; private set; }

    public Guid? OrderId { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }
}

public enum StockMovementType
{
    Reserved = 1,
    Released = 2,
    Deducted = 3,
    ReservationExpired = 4,
}
