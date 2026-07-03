using ECommerce.Basket.Data;
using ECommerce.Basket.Models;
using FluentValidation;

namespace ECommerce.Basket.Features.RemoveItemFromBasket;

public sealed class RemoveItemFromBasketHandler(IBasketStore basketStore)
{
    public async Task<IResult> Handle(RemoveItemFromBasketCommand command, CancellationToken cancellationToken)
    {
        var validation = await new RemoveItemFromBasketValidator().ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var sku = BasketItem.NormalizeSku(command.Sku);
        await basketStore.RemoveItem(command.CustomerId, sku, cancellationToken);

        return Results.Ok(new RemoveItemFromBasketResponse(command.CustomerId, sku));
    }
}
