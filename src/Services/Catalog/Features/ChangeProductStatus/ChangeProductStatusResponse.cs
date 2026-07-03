using ECommerce.Catalog.Models;

namespace ECommerce.Catalog.Features.ChangeProductStatus;

public sealed record ChangeProductStatusResponse(Guid Id, ProductStatus Status);
