using ECommerce.Catalog.Models;

namespace ECommerce.Catalog.Features.GetProduct;

public sealed record GetProductResponse(
    Guid Id,
    string Name,
    string Sku,
    string? Description,
    decimal ListPrice,
    Guid CategoryId,
    Guid BrandId,
    ProductStatus Status);
