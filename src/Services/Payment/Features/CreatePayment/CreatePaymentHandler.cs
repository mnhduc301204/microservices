using ECommerce.Payment.Data;
using ECommerce.ServiceDefaults.Messaging;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ECommerce.Payment.Features.CreatePayment;

public sealed class CreatePaymentHandler(PaymentDbContext dbContext)
{
    public async Task<IResult> Handle(CreatePaymentCommand command, string? idempotencyKey, CancellationToken cancellationToken)
    {
        var validation = await new CreatePaymentValidator().ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var replay = await dbContext.Set<IdempotencyRecord>()
                .AsNoTracking()
                .FirstOrDefaultAsync(record => record.ServiceName == "payment" && record.IdempotencyKey == idempotencyKey, cancellationToken);
            if (replay is not null)
            {
                var cached = JsonSerializer.Deserialize<CreatePaymentResponse>(replay.ResponseBody ?? string.Empty);
                return Results.Json(cached, statusCode: replay.StatusCode);
            }
        }

        var existing = await dbContext.Payments.AsNoTracking().FirstOrDefaultAsync(payment => payment.OrderId == command.OrderId, cancellationToken);
        if (existing is not null)
        {
            return Results.Ok(new CreatePaymentResponse(existing.Id, existing.OrderId, existing.Status));
        }

        var payment = new Models.Payment(
            command.OrderId,
            command.Amount,
            command.Currency,
            idempotencyKey,
            command.CustomerId,
            command.CustomerEmail);
        dbContext.Payments.Add(payment);
        var response = new CreatePaymentResponse(payment.Id, payment.OrderId, payment.Status);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            dbContext.Set<IdempotencyRecord>().Add(new IdempotencyRecord(
                "payment",
                idempotencyKey,
                StatusCodes.Status201Created,
                JsonSerializer.Serialize(response)));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/payments/{payment.Id}", response);
    }
}
