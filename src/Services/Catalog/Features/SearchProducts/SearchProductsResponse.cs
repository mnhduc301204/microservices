using ECommerce.Catalog.Contracts;

namespace ECommerce.Catalog.Features.SearchProducts;

public sealed record SearchProductsResponse(IReadOnlyCollection<ProductDto> Items, int PageNumber, int PageSize, int TotalCount);
