using ECommerce.Catalog.Contracts;
using ECommerce.Catalog.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Features.SearchProducts;

public sealed class SearchProductsHandler(CatalogDbContext dbContext)
{
    public async Task<IResult> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(request.PageNumber, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = dbContext.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(product => product.Name.Contains(term) || product.Sku.Contains(term));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(product => product.Status == request.Status.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(product => product.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(product => new ProductDto(
                product.Id,
                product.Name,
                product.Sku,
                product.Description,
                product.ListPrice,
                product.CategoryId,
                product.BrandId,
                product.Status))
            .ToListAsync(cancellationToken);

        return Results.Ok(new SearchProductsResponse(items, pageNumber, pageSize, total));
    }
}
