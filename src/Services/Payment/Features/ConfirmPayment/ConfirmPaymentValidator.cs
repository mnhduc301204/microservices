using FluentValidation;

namespace ECommerce.Payment.Features.ConfirmPayment;

public sealed class ConfirmPaymentValidator : AbstractValidator<ConfirmPaymentCommand>
{
    public ConfirmPaymentValidator()
    {
        RuleFor(command => command.PaymentId).NotEmpty();
    }
}
