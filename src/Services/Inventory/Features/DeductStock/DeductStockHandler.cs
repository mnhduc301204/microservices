using ECommerce.Inventory.Data;
using ECommerce.Inventory.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Inventory.Features.DeductStock;

public sealed class DeductStockHandler(InventoryDbContext dbContext)
{
    public async Task<IResult> Handle(DeductStockCommand command, CancellationToken cancellationToken)
    {
        var validation = await new DeductStockValidator().ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var reservations = await dbContext.Reservations
            .Where(reservation => reservation.ReservationId == command.ReservationId && reservation.Status == StockReservationStatus.Reserved)
            .ToListAsync(cancellationToken);

        if (reservations.Count == 0)
        {
            return Results.NotFound();
        }

        foreach (var reservation in reservations)
        {
            var item = await dbContext.Items.FirstAsync(item => item.Sku == reservation.Sku, cancellationToken);
            item.DeductReserved(reservation.Quantity);
            reservation.Deduct();
            dbContext.StockMovements.Add(new StockMovement(
                reservation.Sku,
                reservation.Quantity,
                StockMovementType.Deducted,
                reservation.ReservationId,
                reservation.OrderId,
                "Reserved stock deducted."));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(new DeductStockResponse(command.ReservationId));
    }
}
