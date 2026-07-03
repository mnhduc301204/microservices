namespace ECommerce.Basket.Features.AddItemToBasket;

public sealed record AddItemToBasketCommand(Guid CustomerId, string Sku, string ProductName, decimal UnitPrice, int Quantity);
