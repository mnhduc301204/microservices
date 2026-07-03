using FluentValidation;

namespace ECommerce.Ordering.Features.CancelOrder;

public sealed class CancelOrderValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderValidator()
    {
        RuleFor(command => command.OrderId).NotEmpty();
    }
}
