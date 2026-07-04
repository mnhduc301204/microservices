using System.Security.Cryptography;
using System.Text;
using ECommerce.Contracts;
using ECommerce.Payment.Data;
using ECommerce.Payment.Features.HandlePaymentWebhook;
using ECommerce.Payment.Models;
using ECommerce.ServiceDefaults.Messaging;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using PaymentEntity = ECommerce.Payment.Models.Payment;

namespace ECommerce.Payment.Tests;

public sealed class PaymentWebhookEdgeTests
{
    private const string Secret = "test-secret";

    [Fact]
    public async Task Webhook_WhenPaymentDoesNotExist_ReturnsNotFoundAndDoesNotRecordEvent()
    {
        await using var dbContext = CreateDbContext();
        var command = CreateCommand(Guid.NewGuid(), "provider-tx-1", "SUCCEEDED");

        var result = await CreateHandler(dbContext).Handle(command, CancellationToken.None);

        result.GetType().Name.Should().Contain("NotFound");
        (await dbContext.WebhookEvents.CountAsync()).Should().Be(0);
        (await dbContext.Set<OutboxMessage>().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Webhook_WhenStatusIsUnsupported_DoesNotMutatePaymentOrRecordEvent()
    {
        await using var dbContext = CreateDbContext();
        var payment = new PaymentEntity(Guid.NewGuid(), 10m, "USD");
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync();

        var result = await CreateHandler(dbContext).Handle(
            CreateCommand(payment.Id, "provider-tx-1", "PENDING"),
            CancellationToken.None);

        result.GetType().Name.Should().Contain("BadRequest");
        (await dbContext.Payments.SingleAsync()).Status.Should().Be(PaymentStatus.Pending);
        (await dbContext.WebhookEvents.CountAsync()).Should().Be(0);
        (await dbContext.Set<OutboxMessage>().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Webhook_WhenSucceededEventIsRetriedWithDifferentEventId_DoesNotPublishDuplicateSuccess()
    {
        await using var dbContext = CreateDbContext();
        var payment = new PaymentEntity(Guid.NewGuid(), 10m, "USD");
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        await handler.Handle(CreateCommand(payment.Id, "provider-tx-1", "SUCCEEDED"), CancellationToken.None);
        await handler.Handle(CreateCommand(payment.Id, "provider-tx-1", "SUCCEEDED"), CancellationToken.None);

        (await dbContext.Payments.SingleAsync()).Status.Should().Be(PaymentStatus.Succeeded);
        (await dbContext.Set<OutboxMessage>().CountAsync(message => message.Topic == KafkaTopics.PaymentSucceeded)).Should().Be(1);
        (await dbContext.WebhookEvents.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Webhook_WhenFailedEventArrivesAfterSuccess_DoesNotRollbackPaymentOrPublishFailure()
    {
        await using var dbContext = CreateDbContext();
        var payment = new PaymentEntity(Guid.NewGuid(), 10m, "USD");
        payment.MarkSucceeded("provider-tx-1");
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync();

        await CreateHandler(dbContext).Handle(
            CreateCommand(payment.Id, "provider-tx-2", "FAILED"),
            CancellationToken.None);

        (await dbContext.Payments.SingleAsync()).Status.Should().Be(PaymentStatus.Succeeded);
        (await dbContext.Set<OutboxMessage>().CountAsync(message => message.Topic == KafkaTopics.PaymentFailed)).Should().Be(0);
        (await dbContext.WebhookEvents.CountAsync()).Should().Be(1);
    }

    private static PaymentWebhookHandler CreateHandler(PaymentDbContext dbContext)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PaymentProvider:WebhookSecret"] = Secret,
            })
            .Build();

        return new PaymentWebhookHandler(dbContext, configuration, new TestHostEnvironment());
    }

    private static PaymentWebhookCommand CreateCommand(Guid paymentId, string providerTransactionId, string status)
    {
        var eventId = Guid.NewGuid();
        var payload = $"{eventId}:{paymentId}:{providerTransactionId}:{status}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
        var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        return new PaymentWebhookCommand(eventId, paymentId, providerTransactionId, status, signature, DateTimeOffset.UtcNow);
    }

    private static PaymentDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PaymentDbContext(options);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "Payment.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
