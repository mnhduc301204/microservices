using ECommerce.Basket.Data;

namespace ECommerce.Basket.Features.GetBasket;

public static class GetBasketEndpoint
{
    public static RouteGroupBuilder MapGetBasket(this RouteGroupBuilder group)
    {
        group.MapGet("/{customerId:guid}", async (Guid customerId, IBasketStore basketStore, CancellationToken cancellationToken) =>
            await new GetBasketHandler(basketStore).Handle(new GetBasketQuery(customerId), cancellationToken));

        return group;
    }
}
