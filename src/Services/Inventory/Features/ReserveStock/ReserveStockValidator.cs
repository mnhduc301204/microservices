using FluentValidation;

namespace ECommerce.Inventory.Features.ReserveStock;

public sealed class ReserveStockValidator : AbstractValidator<ReserveStockCommand>
{
    public ReserveStockValidator()
    {
        RuleFor(command => command.ReservationId).NotEmpty();
        RuleFor(command => command.Lines).NotEmpty();
        RuleForEach(command => command.Lines).ChildRules(line =>
        {
            line.RuleFor(item => item.Sku).NotEmpty().MaximumLength(64);
            line.RuleFor(item => item.Quantity).GreaterThan(0);
        });
    }
}
