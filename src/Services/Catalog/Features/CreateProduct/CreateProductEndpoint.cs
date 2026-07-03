using ECommerce.ServiceDefaults;
using MassTransit;

namespace ECommerce.Catalog.Features.CreateProduct;

public static class CreateProductEndpoint
{
    public static RouteGroupBuilder MapCreateProduct(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreateProductCommand command, IRequestClient<CreateProductCommand> client, CancellationToken cancellationToken) =>
        {
            var response = await client.GetResponse<OperationResult<CreateProductResponse>>(command, cancellationToken);
            return response.Message.ToHttpResult();
        });

        return group;
    }
}
