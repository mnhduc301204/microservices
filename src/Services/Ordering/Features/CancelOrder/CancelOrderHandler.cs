using ECommerce.Ordering.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Features.CancelOrder;

public sealed class CancelOrderHandler(OrderingDbContext dbContext)
{
    public async Task<IResult> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        var validation = await new CancelOrderValidator().ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var order = await dbContext.Orders.FirstOrDefaultAsync(order => order.Id == command.OrderId, cancellationToken);
        if (order is null)
        {
            return Results.NotFound();
        }

        try
        {
            order.Cancel();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(new CancelOrderResponse(order.Id, order.Status));
    }
}
