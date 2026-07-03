using ECommerce.Catalog.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Features.GetProduct;

public sealed class GetProductHandler(CatalogDbContext dbContext)
{
    public async Task<IResult> Handle(GetProductQuery query, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.AsNoTracking().FirstOrDefaultAsync(product => product.Id == query.Id, cancellationToken);
        if (product is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new GetProductResponse(
            product.Id,
            product.Name,
            product.Sku,
            product.Description,
            product.ListPrice,
            product.CategoryId,
            product.BrandId,
            product.Status));
    }
}
