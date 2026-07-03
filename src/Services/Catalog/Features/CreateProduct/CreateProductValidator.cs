using FluentValidation;

namespace ECommerce.Catalog.Features.CreateProduct;

public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Sku).NotEmpty().MaximumLength(64);
        RuleFor(command => command.ListPrice).GreaterThanOrEqualTo(0);
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.BrandId).NotEmpty();
        RuleFor(command => command.Description).MaximumLength(2000);
    }
}
