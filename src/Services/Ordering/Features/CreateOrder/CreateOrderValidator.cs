using FluentValidation;

namespace ECommerce.Ordering.Features.CreateOrder;

public sealed class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.CustomerEmail).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(command => command.Items).NotEmpty();
        RuleForEach(command => command.Items).ChildRules(item =>
        {
            item.RuleFor(line => line.Sku).NotEmpty().MaximumLength(64);
            item.RuleFor(line => line.ProductName).NotEmpty().MaximumLength(200);
            item.RuleFor(line => line.UnitPrice).GreaterThanOrEqualTo(0);
            item.RuleFor(line => line.Quantity).GreaterThan(0);
        });
    }
}
