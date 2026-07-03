using ECommerce.Catalog.Data;
using ECommerce.ServiceDefaults;
using MassTransit;

namespace ECommerce.Catalog.Features.CreateProduct;

public sealed class CreateProductConsumer(CatalogDbContext dbContext) : IConsumer<CreateProductCommand>
{
    public async Task Consume(ConsumeContext<CreateProductCommand> context)
    {
        var result = await new CreateProductHandler(dbContext).Execute(context.Message, context.CancellationToken);
        await context.RespondAsync(result);
    }
}
