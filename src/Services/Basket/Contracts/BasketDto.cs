namespace ECommerce.Basket.Contracts;

public sealed record BasketDto(Guid CustomerId, IReadOnlyCollection<BasketItemDto> Items, decimal Total);

public sealed record BasketItemDto(string Sku, string ProductName, decimal UnitPrice, int Quantity);
