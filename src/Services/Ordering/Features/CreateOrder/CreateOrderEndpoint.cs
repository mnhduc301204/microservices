using ECommerce.ServiceDefaults;
using MassTransit;

namespace ECommerce.Ordering.Features.CreateOrder;

public static class CreateOrderEndpoint
{
    public static RouteGroupBuilder MapCreateOrder(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreateOrderCommand command, IRequestClient<CreateOrderCommand> client, CancellationToken cancellationToken) =>
        {
            var response = await client.GetResponse<OperationResult<CreateOrderResponse>>(command, cancellationToken);
            return response.Message.ToHttpResult();
        });

        return group;
    }
}
