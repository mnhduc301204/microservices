using ECommerce.Basket.Data;
using FluentValidation;

namespace ECommerce.Basket.Features.ClearBasket;

public sealed class ClearBasketHandler(IBasketStore basketStore)
{
    public async Task<IResult> Handle(ClearBasketCommand command, CancellationToken cancellationToken)
    {
        var validation = await new ClearBasketValidator().ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        await basketStore.Clear(command.CustomerId, cancellationToken);

        return Results.Ok(new ClearBasketResponse(command.CustomerId));
    }
}
