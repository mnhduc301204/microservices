using ECommerce.Payment.Data;
using ECommerce.Contracts;
using ECommerce.Contracts.Payment;
using ECommerce.ServiceDefaults.Messaging;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Payment.Features.ConfirmPayment;

public sealed class ConfirmPaymentHandler(PaymentDbContext dbContext)
{
    public async Task<IResult> Handle(ConfirmPaymentCommand command, CancellationToken cancellationToken)
    {
        var validation = await new ConfirmPaymentValidator().ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var payment = await dbContext.Payments.FirstOrDefaultAsync(payment => payment.Id == command.PaymentId, cancellationToken);
        if (payment is null)
        {
            return Results.NotFound();
        }

        if (command.ShouldSucceed)
        {
            if (payment.Status == Models.PaymentStatus.Succeeded)
            {
                return Results.Ok(new ConfirmPaymentResponse(payment.Id, payment.OrderId, payment.Status));
            }

            payment.MarkSucceeded($"fake-{payment.Id:N}");
            dbContext.Set<OutboxMessage>().Add(OutboxMessage.Create(
                KafkaTopics.PaymentSucceeded,
                new PaymentSucceededIntegrationEvent(
                    Guid.NewGuid(),
                    DateTimeOffset.UtcNow,
                    payment.Id,
                    payment.OrderId,
                    payment.CustomerId,
                    payment.CustomerEmail,
                    payment.Amount)));
        }
        else
        {
            if (payment.Status == Models.PaymentStatus.Failed || payment.Status == Models.PaymentStatus.Succeeded)
            {
                return Results.Ok(new ConfirmPaymentResponse(payment.Id, payment.OrderId, payment.Status));
            }

            payment.MarkFailed("Payment was declined by fake provider.");
            dbContext.Set<OutboxMessage>().Add(OutboxMessage.Create(
                KafkaTopics.PaymentFailed,
                new PaymentFailedIntegrationEvent(
                    Guid.NewGuid(),
                    DateTimeOffset.UtcNow,
                    payment.Id,
                    payment.OrderId,
                    payment.CustomerId,
                    "Payment was declined by fake provider.")));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(new ConfirmPaymentResponse(payment.Id, payment.OrderId, payment.Status));
    }
}
