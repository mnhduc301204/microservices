using ECommerce.Catalog.Data;

namespace ECommerce.Catalog.Features.ChangeProductStatus;

public static class ChangeProductStatusEndpoint
{
    public static RouteGroupBuilder MapChangeProductStatus(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/status", async (Guid id, ChangeProductStatusCommand command, CatalogDbContext dbContext, CancellationToken cancellationToken) =>
        {
            if (id != command.Id)
            {
                return Results.BadRequest(new { error = "Route id does not match command id." });
            }

            return await new ChangeProductStatusHandler(dbContext).Handle(command, cancellationToken);
        });

        return group;
    }
}
