using ECommerce.Payment.Models;

namespace ECommerce.Payment.Features.HandlePaymentWebhook;

public sealed record PaymentWebhookResponse(Guid PaymentId, PaymentStatus Status, bool Duplicate);
