namespace ECommerce.Basket.Features.AddItemToBasket;

public sealed record AddItemToBasketResponse(Guid CustomerId, string Sku, int Quantity);
