using ECommerce.ServiceDefaults;
using MassTransit;

namespace ECommerce.Basket.Features.AddItemToBasket;

public static class AddItemToBasketEndpoint
{
    public static RouteGroupBuilder MapAddItemToBasket(this RouteGroupBuilder group)
    {
        group.MapPost("/items", async (AddItemToBasketCommand command, IRequestClient<AddItemToBasketCommand> client, CancellationToken cancellationToken) =>
        {
            var response = await client.GetResponse<OperationResult<AddItemToBasketResponse>>(command, cancellationToken);
            return response.Message.ToHttpResult();
        });

        return group;
    }
}
