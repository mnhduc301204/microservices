using ECommerce.Catalog.Data;
using ECommerce.Catalog.Models;

namespace ECommerce.Catalog.Features.SearchProducts;

public static class SearchProductsEndpoint
{
    public static RouteGroupBuilder MapSearchProducts(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            CatalogDbContext dbContext,
            CancellationToken cancellationToken,
            string? searchTerm = null,
            ProductStatus? status = null,
            int pageNumber = 1,
            int pageSize = 20) =>
            await new SearchProductsHandler(dbContext).Handle(
                new SearchProductsQuery(searchTerm, status, pageNumber, pageSize),
                cancellationToken));

        return group;
    }
}
