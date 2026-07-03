using FluentValidation;

namespace ECommerce.Payment.Features.CreatePayment;

public sealed class CreatePaymentValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentValidator()
    {
        RuleFor(command => command.OrderId).NotEmpty();
        RuleFor(command => command.Amount).GreaterThan(0);
        RuleFor(command => command.Currency).NotEmpty().Length(3);
    }
}
