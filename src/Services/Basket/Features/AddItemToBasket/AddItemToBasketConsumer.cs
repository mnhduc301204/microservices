using ECommerce.Basket.Data;
using ECommerce.ServiceDefaults;
using MassTransit;

namespace ECommerce.Basket.Features.AddItemToBasket;

public sealed class AddItemToBasketConsumer(IBasketStore basketStore) : IConsumer<AddItemToBasketCommand>
{
    public async Task Consume(ConsumeContext<AddItemToBasketCommand> context)
    {
        var result = await new AddItemToBasketHandler(basketStore).Execute(context.Message, context.CancellationToken);
        await context.RespondAsync(result);
    }
}
