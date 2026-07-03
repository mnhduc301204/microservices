using ECommerce.Inventory.Data;

namespace ECommerce.Inventory.Features.GetStock;

public static class GetStockEndpoint
{
    public static RouteGroupBuilder MapGetStock(this RouteGroupBuilder group)
    {
        group.MapGet("/{sku}", async (string sku, InventoryDbContext dbContext, CancellationToken cancellationToken) =>
            await new GetStockHandler(dbContext).Handle(new GetStockQuery(sku), cancellationToken));

        return group;
    }
}
