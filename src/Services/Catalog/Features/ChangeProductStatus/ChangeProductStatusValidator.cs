using FluentValidation;

namespace ECommerce.Catalog.Features.ChangeProductStatus;

public sealed class ChangeProductStatusValidator : AbstractValidator<ChangeProductStatusCommand>
{
    public ChangeProductStatusValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Status).IsInEnum();
    }
}
