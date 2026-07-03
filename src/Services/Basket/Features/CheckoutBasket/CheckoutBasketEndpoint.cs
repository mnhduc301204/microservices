using ECommerce.ServiceDefaults;
using MassTransit;

namespace ECommerce.Basket.Features.CheckoutBasket;

public static class CheckoutBasketEndpoint
{
    public static RouteGroupBuilder MapCheckoutBasket(this RouteGroupBuilder group)
    {
        group.MapPost("/{customerId:guid}/checkout", async (
            Guid customerId,
            CheckoutBasketCommand command,
            IRequestClient<CheckoutBasketCommand> client,
            CancellationToken cancellationToken) =>
        {
            if (customerId != command.CustomerId)
            {
                return Results.BadRequest(new { error = "Route customerId does not match command customerId." });
            }

            var response = await client.GetResponse<OperationResult<CheckoutBasketResponse>>(command, cancellationToken);
            return response.Message.ToHttpResult();
        });

        return group;
    }
}
