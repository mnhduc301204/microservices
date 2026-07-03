namespace ECommerce.Payment.Features.CreatePayment;

public sealed record CreatePaymentCommand(Guid OrderId, decimal Amount, string Currency = "USD");
