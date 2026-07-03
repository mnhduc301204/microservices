namespace ECommerce.Inventory.Models;

public sealed class StockReservation
{
    private StockReservation()
    {
    }

    public StockReservation(Guid reservationId, string sku, int quantity, Guid? orderId, DateTimeOffset? expiresAt = null)
    {
        ReservationId = reservationId;
        Sku = InventoryItem.NormalizeSku(sku);
        Quantity = quantity;
        OrderId = orderId;
        Status = StockReservationStatus.Reserved;
        CreatedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt ?? CreatedAt.AddMinutes(15);
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid ReservationId { get; private set; }

    public Guid? OrderId { get; private set; }

    public string Sku { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    public StockReservationStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public void Release() => Status = StockReservationStatus.Released;

    public void Deduct()
    {
        Status = StockReservationStatus.Deducted;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkReleased()
    {
        Status = StockReservationStatus.Released;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}

public enum StockReservationStatus
{
    Reserved = 1,
    Released = 2,
    Deducted = 3,
}
