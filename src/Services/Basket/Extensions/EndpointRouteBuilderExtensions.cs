using ECommerce.Basket.Features.AddItemToBasket;
using ECommerce.Basket.Features.ClearBasket;
using ECommerce.Basket.Features.CheckoutBasket;
using ECommerce.Basket.Features.GetBasket;
using ECommerce.Basket.Features.RemoveItemFromBasket;

namespace ECommerce.Basket.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapBasketEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/basket").WithTags("Basket");

        group.MapGetBasket();
        group.MapAddItemToBasket();
        group.MapRemoveItemFromBasket();
        group.MapClearBasket();
        group.MapCheckoutBasket();

        return app;
    }
}
