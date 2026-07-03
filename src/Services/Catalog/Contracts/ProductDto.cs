using ECommerce.Catalog.Models;

namespace ECommerce.Catalog.Contracts;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Sku,
    string? Description,
    decimal ListPrice,
    Guid CategoryId,
    Guid BrandId,
    ProductStatus Status);
