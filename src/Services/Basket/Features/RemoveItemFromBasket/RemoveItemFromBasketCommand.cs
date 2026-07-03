namespace ECommerce.Basket.Features.RemoveItemFromBasket;

public sealed record RemoveItemFromBasketCommand(Guid CustomerId, string Sku);
