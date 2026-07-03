using ECommerce.Basket.Data;
using ECommerce.Contracts.Basket;
using ECommerce.ServiceDefaults;
using MassTransit;

namespace ECommerce.Basket.Features.CheckoutBasket;

public sealed class CheckoutBasketConsumer(
    IBasketStore basketStore,
    ITopicProducer<BasketCheckedOutIntegrationEvent> producer)
    : IConsumer<CheckoutBasketCommand>
{
    public async Task Consume(ConsumeContext<CheckoutBasketCommand> context)
    {
        var result = await new CheckoutBasketHandler(basketStore, producer).Execute(context.Message, context.CancellationToken);
        await context.RespondAsync(result);
    }
}
