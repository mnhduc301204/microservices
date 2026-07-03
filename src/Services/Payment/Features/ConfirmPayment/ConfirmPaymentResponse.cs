using ECommerce.Payment.Models;

namespace ECommerce.Payment.Features.ConfirmPayment;

public sealed record ConfirmPaymentResponse(Guid PaymentId, Guid OrderId, PaymentStatus Status);
