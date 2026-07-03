using System.Security.Cryptography;
using System.Text;
using ECommerce.Contracts;
using ECommerce.Contracts.Payment;
using ECommerce.Payment.Data;
using ECommerce.Payment.Models;
using ECommerce.ServiceDefaults.Messaging;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Payment.Features.HandlePaymentWebhook;

public sealed class PaymentWebhookHandler(PaymentDbContext dbContext, IConfiguration configuration)
{
    public async Task<IResult> Handle(PaymentWebhookCommand command, CancellationToken cancellationToken)
    {
        if (!IsValidSignature(command))
        {
            return Results.Unauthorized();
        }

        var payment = await dbContext.Payments.FirstOrDefaultAsync(payment => payment.Id == command.PaymentId, cancellationToken);
        if (payment is null)
        {
            return Results.NotFound();
        }

        var existingEvent = await dbContext.WebhookEvents.AsNoTracking()
            .AnyAsync(webhook => webhook.EventId == command.EventId, cancellationToken);
        if (existingEvent)
        {
            return Results.Ok(new PaymentWebhookResponse(payment.Id, payment.Status, true));
        }

        var normalizedStatus = command.Status.Trim().ToUpperInvariant();
        if (normalizedStatus == "SUCCEEDED")
        {
            payment.MarkSucceeded(command.ProviderTransactionId);
            dbContext.Set<OutboxMessage>().Add(OutboxMessage.Create(
                KafkaTopics.PaymentSucceeded,
                new PaymentSucceededIntegrationEvent(
                    Guid.NewGuid(),
                    DateTimeOffset.UtcNow,
                    payment.Id,
                    payment.OrderId,
                    Guid.Empty,
                    command.ProviderTransactionId,
                    payment.Amount)));
        }
        else if (normalizedStatus == "FAILED")
        {
            payment.MarkFailed("Payment provider reported failure.");
            dbContext.Set<OutboxMessage>().Add(OutboxMessage.Create(
                KafkaTopics.PaymentFailed,
                new PaymentFailedIntegrationEvent(
                    Guid.NewGuid(),
                    DateTimeOffset.UtcNow,
                    payment.Id,
                    payment.OrderId,
                    Guid.Empty,
                    "Payment provider reported failure.")));
        }
        else
        {
            return Results.BadRequest(new { error = "Unsupported webhook status." });
        }

        dbContext.WebhookEvents.Add(new PaymentWebhookEvent(command.EventId, payment.Id, command.ProviderTransactionId, normalizedStatus));
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new PaymentWebhookResponse(payment.Id, payment.Status, false));
    }

    private bool IsValidSignature(PaymentWebhookCommand command)
    {
        var secret = configuration["PaymentProvider:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            secret = "development-only-payment-webhook-secret";
        }

        var payload = $"{command.EventId}:{command.PaymentId}:{command.ProviderTransactionId}:{command.Status}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(command.Signature.Trim().ToLowerInvariant()));
    }
}
