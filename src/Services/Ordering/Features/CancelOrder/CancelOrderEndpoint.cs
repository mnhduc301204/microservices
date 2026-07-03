using ECommerce.Ordering.Data;

namespace ECommerce.Ordering.Features.CancelOrder;

public static class CancelOrderEndpoint
{
    public static RouteGroupBuilder MapCancelOrder(this RouteGroupBuilder group)
    {
        group.MapPost("/{orderId:guid}/cancel", async (Guid orderId, OrderingDbContext dbContext, CancellationToken cancellationToken) =>
            await new CancelOrderHandler(dbContext).Handle(new CancelOrderCommand(orderId), cancellationToken));

        return group;
    }
}
