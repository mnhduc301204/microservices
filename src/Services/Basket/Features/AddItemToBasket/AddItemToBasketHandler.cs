using ECommerce.Basket.Data;
using ECommerce.Basket.Models;
using ECommerce.ServiceDefaults;
using FluentValidation;

namespace ECommerce.Basket.Features.AddItemToBasket;

public sealed class AddItemToBasketHandler(IBasketStore basketStore)
{
    public async Task<IResult> Handle(AddItemToBasketCommand command, CancellationToken cancellationToken)
    {
        return (await Execute(command, cancellationToken)).ToHttpResult();
    }

    public async Task<OperationResult<AddItemToBasketResponse>> Execute(AddItemToBasketCommand command, CancellationToken cancellationToken)
    {
        var validation = await new AddItemToBasketValidator().ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return OperationResult<AddItemToBasketResponse>.Validation(validation.ToDictionary());
        }

        var item = await basketStore.AddOrUpdateItem(
            command.CustomerId,
            command.Sku,
            command.ProductName,
            command.UnitPrice,
            command.Quantity,
            cancellationToken);

        return OperationResult<AddItemToBasketResponse>.Ok(new AddItemToBasketResponse(command.CustomerId, item.Sku, item.Quantity));
    }
}
