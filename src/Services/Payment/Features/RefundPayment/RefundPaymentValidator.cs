using FluentValidation;

namespace ECommerce.Payment.Features.RefundPayment;

public sealed class RefundPaymentValidator : AbstractValidator<RefundPaymentCommand>
{
    public RefundPaymentValidator()
    {
        RuleFor(command => command.PaymentId).NotEmpty();
    }
}
