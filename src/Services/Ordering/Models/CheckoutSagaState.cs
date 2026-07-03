namespace ECommerce.Ordering.Models;

public sealed class CheckoutSagaState
{
    private CheckoutSagaState()
    {
    }

    public CheckoutSagaState(Guid checkoutId, Guid orderId, Guid customerId)
    {
        CheckoutId = checkoutId == Guid.Empty ? throw new ArgumentException("Checkout id is required.", nameof(checkoutId)) : checkoutId;
        OrderId = orderId == Guid.Empty ? throw new ArgumentException("Order id is required.", nameof(orderId)) : orderId;
        CustomerId = customerId == Guid.Empty ? throw new ArgumentException("Customer id is required.", nameof(customerId)) : customerId;
        Status = CheckoutSagaStatus.OrderCreated;
        CurrentStep = "OrderCreated";
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid CheckoutId { get; private set; }

    public Guid OrderId { get; private set; }

    public Guid CustomerId { get; private set; }

    public CheckoutSagaStatus Status { get; private set; }

    public string CurrentStep { get; private set; } = string.Empty;

    public Guid? StockReservationId { get; private set; }

    public Guid? PaymentId { get; private set; }

    public string? FailureReason { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public void MarkStockReserved(Guid reservationId)
    {
        if (Status is CheckoutSagaStatus.Completed or CheckoutSagaStatus.Failed)
        {
            return;
        }

        StockReservationId = reservationId;
        Status = CheckoutSagaStatus.StockReserved;
        CurrentStep = "StockReserved";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkPaymentSucceeded(Guid paymentId)
    {
        if (Status == CheckoutSagaStatus.Completed)
        {
            return;
        }

        PaymentId = paymentId;
        Status = CheckoutSagaStatus.Completed;
        CurrentStep = "PaymentSucceeded";
        UpdatedAt = DateTimeOffset.UtcNow;
        CompletedAt = UpdatedAt;
    }

    public void MarkFailed(string reason)
    {
        if (Status == CheckoutSagaStatus.Completed)
        {
            return;
        }

        Status = CheckoutSagaStatus.Failed;
        CurrentStep = "Failed";
        FailureReason = string.IsNullOrWhiteSpace(reason) ? "Checkout failed." : reason.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
        CompletedAt = UpdatedAt;
    }
}

public enum CheckoutSagaStatus
{
    OrderCreated = 1,
    StockReserved = 2,
    Completed = 3,
    Failed = 4,
}
