using ECommerce.Catalog.Data;

namespace ECommerce.Catalog.Features.UpdateProduct;

public static class UpdateProductEndpoint
{
    public static RouteGroupBuilder MapUpdateProduct(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", async (Guid id, UpdateProductCommand command, CatalogDbContext dbContext, CancellationToken cancellationToken) =>
        {
            if (id != command.Id)
            {
                return Results.BadRequest(new { error = "Route id does not match command id." });
            }

            return await new UpdateProductHandler(dbContext).Handle(command, cancellationToken);
        });

        return group;
    }
}
