namespace ECommerce.Catalog.Features.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    decimal ListPrice,
    Guid CategoryId,
    Guid BrandId,
    string? Description);
