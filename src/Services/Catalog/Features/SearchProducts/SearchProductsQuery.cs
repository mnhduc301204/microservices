using ECommerce.Catalog.Models;

namespace ECommerce.Catalog.Features.SearchProducts;

public sealed record SearchProductsQuery(string? SearchTerm, ProductStatus? Status, int PageNumber = 1, int PageSize = 20);
