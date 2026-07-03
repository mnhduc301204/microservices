using ECommerce.Catalog.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Features.UpdateProduct;

public sealed class UpdateProductHandler(CatalogDbContext dbContext)
{
    public async Task<IResult> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var validation = await new UpdateProductValidator().ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var product = await dbContext.Products.FirstOrDefaultAsync(product => product.Id == command.Id, cancellationToken);
        if (product is null)
        {
            return Results.NotFound();
        }

        product.Update(command.Name, command.ListPrice, command.CategoryId, command.BrandId, command.Description);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new UpdateProductResponse(product.Id));
    }
}
