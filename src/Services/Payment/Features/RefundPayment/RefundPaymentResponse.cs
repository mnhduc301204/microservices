using ECommerce.Payment.Models;

namespace ECommerce.Payment.Features.RefundPayment;

public sealed record RefundPaymentResponse(Guid PaymentId, PaymentStatus Status);
