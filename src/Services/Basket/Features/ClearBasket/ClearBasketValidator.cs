using FluentValidation;

namespace ECommerce.Basket.Features.ClearBasket;

public sealed class ClearBasketValidator : AbstractValidator<ClearBasketCommand>
{
    public ClearBasketValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
    }
}
