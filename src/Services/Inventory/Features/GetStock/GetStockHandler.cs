using ECommerce.Inventory.Data;
using ECommerce.Inventory.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Inventory.Features.GetStock;

public sealed class GetStockHandler(InventoryDbContext dbContext)
{
    public async Task<IResult> Handle(GetStockQuery query, CancellationToken cancellationToken)
    {
        var sku = InventoryItem.NormalizeSku(query.Sku);
        var item = await dbContext.Items.AsNoTracking().FirstOrDefaultAsync(item => item.Sku == sku, cancellationToken);
        if (item is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new GetStockResponse(item.Sku, item.QuantityOnHand, item.QuantityReserved, item.AvailableQuantity));
    }
}
