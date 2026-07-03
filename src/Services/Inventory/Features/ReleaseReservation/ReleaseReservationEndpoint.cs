using ECommerce.Inventory.Data;

namespace ECommerce.Inventory.Features.ReleaseReservation;

public static class ReleaseReservationEndpoint
{
    public static RouteGroupBuilder MapReleaseReservation(this RouteGroupBuilder group)
    {
        group.MapPost("/reservations/{reservationId:guid}/release", async (Guid reservationId, InventoryDbContext dbContext, CancellationToken cancellationToken) =>
            await new ReleaseReservationHandler(dbContext).Handle(new ReleaseReservationCommand(reservationId), cancellationToken));

        return group;
    }
}
