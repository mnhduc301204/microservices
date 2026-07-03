using ECommerce.Ordering.Data;

namespace ECommerce.Ordering.Features.GetOrder;

public static class GetOrderEndpoint
{
    public static RouteGroupBuilder MapGetOrder(this RouteGroupBuilder group)
    {
        group.MapGet("/{orderId:guid}", async (Guid orderId, OrderingDbContext dbContext, CancellationToken cancellationToken) =>
            await new GetOrderHandler(dbContext).Handle(new GetOrderQuery(orderId), cancellationToken));

        return group;
    }
}
