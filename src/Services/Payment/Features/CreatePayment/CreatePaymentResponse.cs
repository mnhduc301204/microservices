using ECommerce.Payment.Models;

namespace ECommerce.Payment.Features.CreatePayment;

public sealed record CreatePaymentResponse(Guid PaymentId, Guid OrderId, PaymentStatus Status);
