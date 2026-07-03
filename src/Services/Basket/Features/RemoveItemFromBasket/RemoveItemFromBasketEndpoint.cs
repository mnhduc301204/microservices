using ECommerce.Basket.Data;

namespace ECommerce.Basket.Features.RemoveItemFromBasket;

public static class RemoveItemFromBasketEndpoint
{
    public static RouteGroupBuilder MapRemoveItemFromBasket(this RouteGroupBuilder group)
    {
        group.MapDelete("/{customerId:guid}/items/{sku}", async (Guid customerId, string sku, IBasketStore basketStore, CancellationToken cancellationToken) =>
            await new RemoveItemFromBasketHandler(basketStore).Handle(new RemoveItemFromBasketCommand(customerId, sku), cancellationToken));

        return group;
    }
}
