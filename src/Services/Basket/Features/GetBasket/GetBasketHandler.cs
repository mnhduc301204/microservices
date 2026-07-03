using ECommerce.Basket.Contracts;
using ECommerce.Basket.Data;

namespace ECommerce.Basket.Features.GetBasket;

public sealed class GetBasketHandler(IBasketStore basketStore)
{
    public async Task<IResult> Handle(GetBasketQuery query, CancellationToken cancellationToken)
    {
        var items = await basketStore.GetItems(query.CustomerId, cancellationToken);

        var basket = new BasketDto(query.CustomerId, items, items.Sum(item => item.UnitPrice * item.Quantity));
        return Results.Ok(new GetBasketResponse(basket));
    }
}
