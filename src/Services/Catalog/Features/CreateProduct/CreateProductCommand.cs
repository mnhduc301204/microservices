namespace ECommerce.Catalog.Features.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string Sku,
    decimal ListPrice,
    Guid CategoryId,
    Guid BrandId,
    string? Description);
