using FluentValidation;

namespace ECommerce.Catalog.Features.UpdateProduct;

public sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.ListPrice).GreaterThanOrEqualTo(0);
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.BrandId).NotEmpty();
        RuleFor(command => command.Description).MaximumLength(2000);
    }
}
