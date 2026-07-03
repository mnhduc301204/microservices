using ECommerce.Inventory.Data;
using ECommerce.Inventory.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Inventory.Features.ReserveStock;

public sealed class ReserveStockHandler(InventoryDbContext dbContext)
{
    public async Task<IResult> Handle(ReserveStockCommand command, CancellationToken cancellationToken)
    {
        var validation = await new ReserveStockValidator().ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var existing = await dbContext.Reservations.AsNoTracking()
            .AnyAsync(reservation => reservation.ReservationId == command.ReservationId, cancellationToken);
        if (existing)
        {
            return Results.Ok(new ReserveStockResponse(command.ReservationId, true, null));
        }

        var normalizedLines = command.Lines
            .GroupBy(line => InventoryItem.NormalizeSku(line.Sku))
            .Select(group => new ReserveStockLine(group.Key, group.Sum(line => line.Quantity)))
            .ToArray();

        foreach (var line in normalizedLines)
        {
            var item = await dbContext.Items.FirstOrDefaultAsync(item => item.Sku == line.Sku, cancellationToken);
            if (item is null || item.AvailableQuantity < line.Quantity)
            {
                return Results.Conflict(new ReserveStockResponse(command.ReservationId, false, $"Insufficient stock for {line.Sku}."));
            }
        }

        foreach (var line in normalizedLines)
        {
            var item = await dbContext.Items.FirstAsync(item => item.Sku == line.Sku, cancellationToken);
            item.TryReserve(line.Quantity);
            dbContext.Reservations.Add(new StockReservation(command.ReservationId, line.Sku, line.Quantity, command.OrderId));
            dbContext.StockMovements.Add(new StockMovement(
                line.Sku,
                line.Quantity,
                StockMovementType.Reserved,
                command.ReservationId,
                command.OrderId,
                "Manual stock reservation."));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new ReserveStockResponse(command.ReservationId, true, null));
    }
}
