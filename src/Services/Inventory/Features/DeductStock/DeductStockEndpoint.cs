using ECommerce.Inventory.Data;

namespace ECommerce.Inventory.Features.DeductStock;

public static class DeductStockEndpoint
{
    public static RouteGroupBuilder MapDeductStock(this RouteGroupBuilder group)
    {
        group.MapPost("/reservations/{reservationId:guid}/deduct", async (Guid reservationId, InventoryDbContext dbContext, CancellationToken cancellationToken) =>
            await new DeductStockHandler(dbContext).Handle(new DeductStockCommand(reservationId), cancellationToken));

        return group;
    }
}
