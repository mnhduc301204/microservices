using FluentValidation;

namespace ECommerce.Ordering.Features.MarkOrderPaymentSucceeded;

public sealed class MarkOrderPaymentSucceededValidator : AbstractValidator<MarkOrderPaymentSucceededCommand>
{
    public MarkOrderPaymentSucceededValidator()
    {
        RuleFor(command => command.EventId).NotEmpty();
        RuleFor(command => command.OrderId).NotEmpty();
        RuleFor(command => command.PaymentId).NotEmpty();
    }
}
