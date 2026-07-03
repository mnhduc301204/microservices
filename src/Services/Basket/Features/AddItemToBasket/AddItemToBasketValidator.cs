using FluentValidation;

namespace ECommerce.Basket.Features.AddItemToBasket;

public sealed class AddItemToBasketValidator : AbstractValidator<AddItemToBasketCommand>
{
    public AddItemToBasketValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.Sku).NotEmpty().MaximumLength(64);
        RuleFor(command => command.ProductName).NotEmpty().MaximumLength(200);
        RuleFor(command => command.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(command => command.Quantity).GreaterThan(0);
    }
}
