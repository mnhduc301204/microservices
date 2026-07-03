namespace ECommerce.Payment.Features.ConfirmPayment;

public sealed record ConfirmPaymentCommand(Guid PaymentId, bool ShouldSucceed = true);
