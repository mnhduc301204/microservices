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

        var checkout = await basketStore.GetPendingCheckout(command.CustomerId, cancellationToken);
        if (checkout is null)
        {
            var items = await basketStore.GetItems(command.CustomerId, cancellationToken);

            if (items.Count == 0)
            {
                return OperationResult<CheckoutBasketResponse>.Conflict("Cannot checkout an empty basket.");
            }

            checkout = await basketStore.CreatePendingCheckout(command.CustomerId, Guid.NewGuid(), items, cancellationToken);
        }

        await producer.Produce(
            command.CustomerId.ToString(),
            new BasketCheckedOutIntegrationEvent(
                checkout.CheckoutId,
                DateTimeOffset.UtcNow,
                command.CustomerId,
                command.CustomerEmail,
                checkout.Items.Select(item => new BasketCheckedOutLine(item.Sku, item.ProductName, item.UnitPrice, item.Quantity)).ToArray()),
            cancellationToken);

        await basketStore.CompleteCheckout(command.CustomerId, checkout.CheckoutId, cancellationToken);

        return OperationResult<CheckoutBasketResponse>.Accepted($"/api/basket/{command.CustomerId}", new CheckoutBasketResponse(checkout.CheckoutId));
    }
}
