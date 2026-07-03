using ECommerce.Ordering.Data;
using ECommerce.ServiceDefaults;
using MassTransit;

namespace ECommerce.Ordering.Features.CreateOrder;

public sealed class CreateOrderConsumer(OrderingDbContext dbContext) : IConsumer<CreateOrderCommand>
{
    public async Task Consume(ConsumeContext<CreateOrderCommand> context)
    {
        var result = await new CreateOrderHandler(dbContext).Execute(context.Message, context.CancellationToken);
        await context.RespondAsync(result);
    }
}
