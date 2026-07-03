namespace ECommerce.Payment.Models;

public sealed class Payment
{
    private Payment()
    {
    }

    public Payment(Guid orderId, decimal amount, string currency, string? idempotencyKey = null)
    {
        OrderId = orderId == Guid.Empty ? throw new ArgumentException("Order id is required.", nameof(orderId)) : orderId;
        Amount = amount <= 0 ? throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be positive.") : amount;
        Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant();
        IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim();
        Status = PaymentStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid OrderId { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = "USD";

    public string? IdempotencyKey { get; private set; }

    public string? ProviderTransactionId { get; private set; }

    public string? ProviderIntentId { get; private set; }

    public string? FailureReason { get; private set; }

    public PaymentStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset? RefundedAt { get; private set; }

    public DateTimeOffset? ProviderRequestedAt { get; private set; }

    public DateTimeOffset? ProviderLockedAt { get; private set; }

    public string? ProviderLockedBy { get; private set; }

    public void RequestProviderIntent(string providerIntentId)
    {
        if (Status != PaymentStatus.Pending)
        {
            return;
        }

        ProviderIntentId = string.IsNullOrWhiteSpace(providerIntentId)
            ? throw new ArgumentException("Provider intent id is required.", nameof(providerIntentId))
            : providerIntentId.Trim();
        ProviderRequestedAt = DateTimeOffset.UtcNow;
    }

    public void MarkSucceeded(string? providerTransactionId = null)
    {
        if (Status == PaymentStatus.Succeeded)
        {
            return;
        }

        if (Status == PaymentStatus.Refunded)
        {
            throw new InvalidOperationException("Cannot succeed a refunded payment.");
        }

        ProviderTransactionId = string.IsNullOrWhiteSpace(providerTransactionId) ? ProviderTransactionId : providerTransactionId.Trim();
        FailureReason = null;
        Status = PaymentStatus.Succeeded;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string? reason = null)
    {
        if (Status == PaymentStatus.Succeeded)
        {
            throw new InvalidOperationException("Cannot fail a succeeded payment.");
        }

        FailureReason = string.IsNullOrWhiteSpace(reason) ? "Payment failed." : reason.Trim();
        Status = PaymentStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Refund()
    {
        if (Status != PaymentStatus.Succeeded)
        {
            throw new InvalidOperationException("Only succeeded payments can be refunded.");
        }

        Status = PaymentStatus.Refunded;
        RefundedAt = DateTimeOffset.UtcNow;
    }
}

public enum PaymentStatus
{
    Pending = 1,
    Succeeded = 2,
    Failed = 3,
    Refunded = 4,
}
