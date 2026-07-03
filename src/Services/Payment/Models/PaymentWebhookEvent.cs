namespace ECommerce.Payment.Models;

public sealed class PaymentWebhookEvent
{
    private PaymentWebhookEvent()
    {
    }

    public PaymentWebhookEvent(Guid eventId, Guid paymentId, string providerTransactionId, string status)
    {
        EventId = eventId == Guid.Empty ? throw new ArgumentException("Event id is required.", nameof(eventId)) : eventId;
        PaymentId = paymentId == Guid.Empty ? throw new ArgumentException("Payment id is required.", nameof(paymentId)) : paymentId;
        ProviderTransactionId = string.IsNullOrWhiteSpace(providerTransactionId)
            ? throw new ArgumentException("Provider transaction id is required.", nameof(providerTransactionId))
            : providerTransactionId.Trim();
        Status = string.IsNullOrWhiteSpace(status) ? throw new ArgumentException("Status is required.", nameof(status)) : status.Trim();
        ProcessedAt = DateTimeOffset.UtcNow;
    }

    public Guid EventId { get; private set; }

    public Guid PaymentId { get; private set; }

    public string ProviderTransactionId { get; private set; } = string.Empty;

    public string Status { get; private set; } = string.Empty;

    public DateTimeOffset ProcessedAt { get; private set; }
}
