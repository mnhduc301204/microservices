namespace ECommerce.Payment.Features.HandlePaymentWebhook;

public sealed record PaymentWebhookCommand(
    Guid EventId,
    Guid PaymentId,
    string ProviderTransactionId,
    string Status,
    string Signature);
