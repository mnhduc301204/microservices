using ECommerce.Inventory.Data;

namespace ECommerce.Inventory.Features.ReserveStock;

public static class ReserveStockEndpoint
{
    public static RouteGroupBuilder MapReserveStock(this RouteGroupBuilder group)
    {
        group.MapPost("/reservations", async (ReserveStockCommand command, InventoryDbContext dbContext, CancellationToken cancellationToken) =>
            await new ReserveStockHandler(dbContext).Handle(command, cancellationToken));

        return group;
    }
}
