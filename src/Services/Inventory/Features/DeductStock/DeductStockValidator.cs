using FluentValidation;

namespace ECommerce.Inventory.Features.DeductStock;

public sealed class DeductStockValidator : AbstractValidator<DeductStockCommand>
{
    public DeductStockValidator()
    {
        RuleFor(command => command.ReservationId).NotEmpty();
    }
}
