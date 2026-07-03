using ECommerce.Payment.Data;
using ECommerce.Payment.Features.CreatePayment;
using ECommerce.Payment.Features.HandlePaymentWebhook;
using ECommerce.Payment.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ECommerce.Payment.Tests;

public sealed class PaymentIdempotencyTests
{
    [Fact]
    public async Task CreatePayment_WithSameIdempotencyKey_CreatesSinglePayment()
    {
        await using var dbContext = CreateDbContext();
        var handler = new CreatePaymentHandler(dbContext);
        var command = new CreatePaymentCommand(Guid.NewGuid(), 25m);

        await handler.Handle(command, "pay-key-1", CancellationToken.None);
        await handler.Handle(command, "pay-key-1", CancellationToken.None);

        (await dbContext.Payments.CountAsync()).Should().Be(1);
        (await dbContext.Set<ECommerce.ServiceDefaults.Messaging.IdempotencyRecord>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public void Payment_WhenSucceededTwice_RemainsSucceededWithProviderTransaction()
    {
        var payment = new ECommerce.Payment.Models.Payment(Guid.NewGuid(), 25m, "usd", "key-1");

        payment.MarkSucceeded("provider-tx-1");
        payment.MarkSucceeded("provider-tx-1");

        payment.Status.Should().Be(PaymentStatus.Succeeded);
        payment.ProviderTransactionId.Should().Be("provider-tx-1");
    }

    [Fact]
    public async Task Webhook_WithStaleTimestamp_IsRejected()
    {
        await using var dbContext = CreateDbContext();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PaymentProvider:WebhookSecret"] = "test-secret",
            })
            .Build();
        var handler = new PaymentWebhookHandler(dbContext, configuration, new TestHostEnvironment());
        var command = new PaymentWebhookCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "provider-tx-1",
            "SUCCEEDED",
            "invalid-signature",
            DateTimeOffset.UtcNow.AddMinutes(-10));

        var result = await handler.Handle(command, CancellationToken.None);

        result.GetType().Name.Should().Contain("Unauthorized");
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
