using ECommerce.Basket.Data;

namespace ECommerce.Basket.Features.ClearBasket;

public static class ClearBasketEndpoint
{
    public static RouteGroupBuilder MapClearBasket(this RouteGroupBuilder group)
    {
        group.MapDelete("/{customerId:guid}", async (Guid customerId, IBasketStore basketStore, CancellationToken cancellationToken) =>
            await new ClearBasketHandler(basketStore).Handle(new ClearBasketCommand(customerId), cancellationToken));

        return group;
    }
}
