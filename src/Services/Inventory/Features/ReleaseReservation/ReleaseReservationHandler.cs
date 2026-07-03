using ECommerce.Inventory.Data;
using ECommerce.Inventory.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Inventory.Features.ReleaseReservation;

public sealed class ReleaseReservationHandler(InventoryDbContext dbContext)
{
    public async Task<IResult> Handle(ReleaseReservationCommand command, CancellationToken cancellationToken)
    {
        var validation = await new ReleaseReservationValidator().ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var reservations = await dbContext.Reservations
            .Where(reservation => reservation.ReservationId == command.ReservationId && reservation.Status == StockReservationStatus.Reserved)
            .ToListAsync(cancellationToken);

        foreach (var reservation in reservations)
        {
            var item = await dbContext.Items.FirstAsync(item => item.Sku == reservation.Sku, cancellationToken);
            item.Release(reservation.Quantity);
            reservation.MarkReleased();
            dbContext.StockMovements.Add(new StockMovement(
                reservation.Sku,
                reservation.Quantity,
                StockMovementType.Released,
                reservation.ReservationId,
                reservation.OrderId,
                "Manual stock reservation release."));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(new ReleaseReservationResponse(command.ReservationId));
    }
}
