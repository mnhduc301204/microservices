using ECommerce.Catalog.Models;

namespace ECommerce.Catalog.Features.ChangeProductStatus;

public sealed record ChangeProductStatusCommand(Guid Id, ProductStatus Status);
