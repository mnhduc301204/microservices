using ECommerce.Ordering.Data;

namespace ECommerce.Ordering.Features.MarkOrderPaymentSucceeded;

public static class MarkOrderPaymentSucceededEndpoint
{
    public static RouteGroupBuilder MapMarkOrderPaymentSucceeded(this RouteGroupBuilder group)
    {
        group.MapPost("/{orderId:guid}/payment-succeeded", async (
            Guid orderId,
            MarkOrderPaymentSucceededCommand command,
            OrderingDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            if (orderId != command.OrderId)
            {
                return Results.BadRequest(new { error = "Route orderId does not match command orderId." });
            }

            return await new MarkOrderPaymentSucceededHandler(dbContext).Handle(command, cancellationToken);
        });

        return group;
    }
}
