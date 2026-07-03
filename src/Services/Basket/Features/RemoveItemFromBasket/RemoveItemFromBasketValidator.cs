using FluentValidation;

namespace ECommerce.Basket.Features.RemoveItemFromBasket;

public sealed class RemoveItemFromBasketValidator : AbstractValidator<RemoveItemFromBasketCommand>
{
    public RemoveItemFromBasketValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.Sku).NotEmpty();
    }
}
