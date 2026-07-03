using ECommerce.Payment.Data;
using ECommerce.ServiceDefaults.Messaging;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ECommerce.Payment.Features.RefundPayment;

public sealed class RefundPaymentHandler(PaymentDbContext dbContext)
{
    public async Task<IResult> Handle(RefundPaymentCommand command, string? idempotencyKey, CancellationToken cancellationToken)
    {
        var validation = await new RefundPaymentValidator().ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var replay = await dbContext.Set<IdempotencyRecord>()
                .AsNoTracking()
                .FirstOrDefaultAsync(record => record.ServiceName == "payment-refund" && record.IdempotencyKey == idempotencyKey, cancellationToken);
            if (replay is not null)
            {
                var cached = JsonSerializer.Deserialize<RefundPaymentResponse>(replay.ResponseBody ?? string.Empty);
                return Results.Json(cached, statusCode: replay.StatusCode);
            }
        }

        var payment = await dbContext.Payments.FirstOrDefaultAsync(payment => payment.Id == command.PaymentId, cancellationToken);
        if (payment is null)
        {
            return Results.NotFound();
        }

        try
        {
            payment.Refund();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }

        var response = new RefundPaymentResponse(payment.Id, payment.Status);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            dbContext.Set<IdempotencyRecord>().Add(new IdempotencyRecord(
                "payment-refund",
                idempotencyKey,
                StatusCodes.Status200OK,
                JsonSerializer.Serialize(response)));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(response);
    }
}
