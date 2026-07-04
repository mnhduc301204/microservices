using ECommerce.Basket.Data;
using ECommerce.Contracts.Basket;
using ECommerce.ServiceDefaults;
using FluentValidation;
using MassTransit;

namespace ECommerce.Basket.Features.CheckoutBasket;

public sealed class CheckoutBasketHandler(IBasketStore basketStore, ITopicProducer<string, BasketCheckedOutIntegrationEvent> producer)
{
    public async Task<IResult> Handle(CheckoutBasketCommand command, CancellationToken cancellationToken)
    {
        return (await Execute(command, cancellationToken)).ToHttpResult();
    }

    public async Task<OperationResult<CheckoutBasketResponse>> Execute(CheckoutBasketCommand command, CancellationToken cancellationToken)
    {
        var validation = await new CheckoutBasketValidator().ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return OperationResult<CheckoutBasketResponse>.Validation(validation.ToDictionary());
        }

        var items = await basketStore.GetItems(command.CustomerId, cancellationToken);

        if (items.Count == 0)
        {
            return OperationResult<CheckoutBasketResponse>.Conflict("Cannot checkout an empty basket.");
        }

        var checkoutId = Guid.NewGuid();
        await producer.Produce(
            command.CustomerId.ToString(),
            new BasketCheckedOutIntegrationEvent(
                checkoutId,
                DateTimeOffset.UtcNow,
                command.CustomerId,
                command.CustomerEmail,
                items.Select(item => new BasketCheckedOutLine(item.Sku, item.ProductName, item.UnitPrice, item.Quantity)).ToArray()),
            cancellationToken);

        await basketStore.Clear(command.CustomerId, cancellationToken);

        return OperationResult<CheckoutBasketResponse>.Accepted($"/api/basket/{command.CustomerId}", new CheckoutBasketResponse(checkoutId));
    }
}
