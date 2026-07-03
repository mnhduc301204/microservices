using FluentValidation;

namespace ECommerce.Basket.Features.CheckoutBasket;

public sealed class CheckoutBasketValidator : AbstractValidator<CheckoutBasketCommand>
{
    public CheckoutBasketValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.CustomerEmail).NotEmpty().EmailAddress().MaximumLength(320);
    }
}
