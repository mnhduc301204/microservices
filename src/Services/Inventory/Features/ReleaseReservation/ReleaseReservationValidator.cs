using FluentValidation;

namespace ECommerce.Inventory.Features.ReleaseReservation;

public sealed class ReleaseReservationValidator : AbstractValidator<ReleaseReservationCommand>
{
    public ReleaseReservationValidator()
    {
        RuleFor(command => command.ReservationId).NotEmpty();
    }
}
