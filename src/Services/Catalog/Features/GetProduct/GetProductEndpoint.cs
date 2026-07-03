using ECommerce.Catalog.Data;

namespace ECommerce.Catalog.Features.GetProduct;

public static class GetProductEndpoint
{
    public static RouteGroupBuilder MapGetProduct(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", async (Guid id, CatalogDbContext dbContext, CancellationToken cancellationToken) =>
            await new GetProductHandler(dbContext).Handle(new GetProductQuery(id), cancellationToken));

        return group;
    }
}
