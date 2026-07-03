using ECommerce.Contracts;
using ECommerce.Contracts.Payment;
using ECommerce.Payment.Data;
using ECommerce.Payment.Models;
using ECommerce.ServiceDefaults.Messaging;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Payment.BackgroundServices;

public sealed class FakePaymentProviderWorker(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<FakePaymentProviderWorker> logger)
    : BackgroundService
{
    private readonly string workerId = $"{Environment.MachineName}-{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingPayments(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process fake payment intents.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessPendingPayments(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var delay = TimeSpan.FromSeconds(configuration.GetValue("PaymentProvider:FakeProcessingDelaySeconds", 2));
        var cutoff = DateTimeOffset.UtcNow.Subtract(delay);
        var lockCutoff = DateTimeOffset.UtcNow.AddMinutes(-5);
        var shouldSucceed = configuration.GetValue("PaymentProvider:FakeAlwaysSucceed", true);

        var paymentIds = await dbContext.Payments
            .Where(payment =>
                payment.Status == PaymentStatus.Pending
                && payment.ProviderIntentId != null
                && (payment.ProviderLockedAt == null || payment.ProviderLockedAt <= lockCutoff)
                && payment.ProviderRequestedAt <= cutoff)
            .OrderBy(payment => payment.ProviderRequestedAt)
            .Take(50)
            .Select(payment => payment.Id)
            .ToListAsync(cancellationToken);

        foreach (var paymentId in paymentIds)
        {
            var claimed = await dbContext.Payments
                .Where(payment =>
                    payment.Id == paymentId
                    && payment.Status == PaymentStatus.Pending
                    && payment.ProviderIntentId != null
                    && (payment.ProviderLockedAt == null || payment.ProviderLockedAt <= lockCutoff))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(payment => payment.ProviderLockedAt, DateTimeOffset.UtcNow)
                    .SetProperty(payment => payment.ProviderLockedBy, workerId), cancellationToken);

            if (claimed == 0)
            {
                continue;
            }

            var payment = await dbContext.Payments.FirstOrDefaultAsync(
                candidate => candidate.Id == paymentId
                    && candidate.Status == PaymentStatus.Pending
                    && candidate.ProviderLockedBy == workerId,
                cancellationToken);

            if (payment is null)
            {
                continue;
            }

            if (shouldSucceed)
            {
                payment.MarkSucceeded($"fake-tx-{payment.Id:N}");
                dbContext.Set<OutboxMessage>().Add(OutboxMessage.Create(
                    KafkaTopics.PaymentSucceeded,
                    new PaymentSucceededIntegrationEvent(
                        Guid.NewGuid(),
                        DateTimeOffset.UtcNow,
                        payment.Id,
                        payment.OrderId,
                        Guid.Empty,
                        payment.ProviderTransactionId ?? string.Empty,
                        payment.Amount)));
            }
            else
            {
                var reason = "Payment was declined by fake provider.";
                payment.MarkFailed(reason);
                dbContext.Set<OutboxMessage>().Add(OutboxMessage.Create(
                    KafkaTopics.PaymentFailed,
                    new PaymentFailedIntegrationEvent(
                        Guid.NewGuid(),
                        DateTimeOffset.UtcNow,
                        payment.Id,
                        payment.OrderId,
                        Guid.Empty,
                        reason)));
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
