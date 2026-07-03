namespace ECommerce.Basket.Features.CheckoutBasket;

public sealed record CheckoutBasketCommand(Guid CustomerId, string CustomerEmail);
