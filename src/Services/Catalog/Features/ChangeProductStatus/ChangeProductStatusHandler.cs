using ECommerce.Catalog.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Features.ChangeProductStatus;

public sealed class ChangeProductStatusHandler(CatalogDbContext dbContext)
{
    public async Task<IResult> Handle(ChangeProductStatusCommand command, CancellationToken cancellationToken)
    {
        var validation = await new ChangeProductStatusValidator().ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var product = await dbContext.Products.FirstOrDefaultAsync(product => product.Id == command.Id, cancellationToken);
        if (product is null)
        {
            return Results.NotFound();
        }

        product.ChangeStatus(command.Status);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new ChangeProductStatusResponse(product.Id, product.Status));
    }
}
