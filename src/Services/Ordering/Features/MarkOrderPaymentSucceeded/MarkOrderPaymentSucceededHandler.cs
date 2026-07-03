using ECommerce.Ordering.Data;
using ECommerce.Ordering.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Features.MarkOrderPaymentSucceeded;

public sealed class MarkOrderPaymentSucceededHandler(OrderingDbContext dbContext)
{
    public async Task<IResult> Handle(MarkOrderPaymentSucceededCommand command, CancellationToken cancellationToken)
    {
        var validation = await new MarkOrderPaymentSucceededValidator().ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        if (await dbContext.ProcessedMessages.AnyAsync(message => message.EventId == command.EventId, cancellationToken))
        {
            var existing = await dbContext.Orders.AsNoTracking().FirstOrDefaultAsync(order => order.Id == command.OrderId, cancellationToken);
            return existing is null ? Results.NotFound() : Results.Ok(new MarkOrderPaymentSucceededResponse(existing.Id, existing.Status));
        }

        var order = await dbContext.Orders.FirstOrDefaultAsync(order => order.Id == command.OrderId, cancellationToken);
        if (order is null)
        {
            return Results.NotFound();
        }

        order.ConfirmPayment();
        dbContext.ProcessedMessages.Add(new ProcessedMessage { EventId = command.EventId });
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new MarkOrderPaymentSucceededResponse(order.Id, order.Status));
    }
}
