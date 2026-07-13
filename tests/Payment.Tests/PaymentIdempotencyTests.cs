using ECommerce.Payment.Data;
using ECommerce.Payment.Consumers;
using ECommerce.Payment.BackgroundServices;
using ECommerce.Payment.Features.ConfirmPayment;
using ECommerce.Payment.Features.CreatePayment;
using ECommerce.Payment.Features.HandlePaymentWebhook;
using ECommerce.Payment.Features.RefundPayment;
using ECommerce.Payment.Models;
using ECommerce.Contracts;
using ECommerce.ServiceDefaults.Messaging;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MassTransit;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;

namespace ECommerce.Payment.Tests;

public sealed class PaymentIdempotencyTests
{
    [Fact]
    public async Task CreatePayment_WithSameIdempotencyKey_CreatesSinglePayment()
    {
        await using var dbContext = CreateDbContext();
        var handler = new CreatePaymentHandler(dbContext);
        var command = new CreatePaymentCommand(Guid.NewGuid(), 25m, "USD", Guid.NewGuid(), "buyer@example.com");

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

    [Fact]
    public async Task ConfirmPayment_WhenProviderDeclines_MarksPaymentFailedAndOutboxesFailureEvent()
    {
        await using var dbContext = CreateDbContext();
        var payment = new ECommerce.Payment.Models.Payment(Guid.NewGuid(), 25m, "USD");
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync();

        await new ConfirmPaymentHandler(dbContext).Handle(
            new ConfirmPaymentCommand(payment.Id, ShouldSucceed: false),
            CancellationToken.None);

        var updated = await dbContext.Payments.SingleAsync();
        updated.Status.Should().Be(PaymentStatus.Failed);
        updated.FailureReason.Should().Be("Payment was declined by fake provider.");

        var outbox = await dbContext.Set<OutboxMessage>().SingleAsync();
        outbox.Topic.Should().Be(KafkaTopics.PaymentFailed);
        outbox.MessageType.Should().Contain("PaymentFailedIntegrationEvent");
    }

    [Fact]
    public async Task ConfirmPayment_WhenSucceededCommandIsRetried_DoesNotOutboxDuplicateSuccessEvent()
    {
        await using var dbContext = CreateDbContext();
        var payment = new ECommerce.Payment.Models.Payment(Guid.NewGuid(), 25m, "USD");
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync();
        var handler = new ConfirmPaymentHandler(dbContext);
        var command = new ConfirmPaymentCommand(payment.Id);

        await handler.Handle(command, CancellationToken.None);
        await handler.Handle(command, CancellationToken.None);

        (await dbContext.Payments.SingleAsync()).Status.Should().Be(PaymentStatus.Succeeded);
        (await dbContext.Set<OutboxMessage>().CountAsync(message => message.Topic == KafkaTopics.PaymentSucceeded)).Should().Be(1);
    }

    [Fact]
    public async Task ConfirmPayment_WhenFailureArrivesAfterSuccess_DoesNotFailPaymentOrPublishFailure()
    {
        await using var dbContext = CreateDbContext();
        var payment = new ECommerce.Payment.Models.Payment(Guid.NewGuid(), 25m, "USD");
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync();
        var handler = new ConfirmPaymentHandler(dbContext);

        await handler.Handle(new ConfirmPaymentCommand(payment.Id), CancellationToken.None);
        await handler.Handle(new ConfirmPaymentCommand(payment.Id, ShouldSucceed: false), CancellationToken.None);

        (await dbContext.Payments.SingleAsync()).Status.Should().Be(PaymentStatus.Succeeded);
        (await dbContext.Set<OutboxMessage>().CountAsync(message => message.Topic == KafkaTopics.PaymentFailed)).Should().Be(0);
    }

    [Fact]
    public async Task RefundPayment_WhenPaymentIsPending_ReturnsConflictAndDoesNotCacheIdempotencyRecord()
    {
        await using var dbContext = CreateDbContext();
        var payment = new ECommerce.Payment.Models.Payment(Guid.NewGuid(), 25m, "USD");
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync();

        var result = await new RefundPaymentHandler(dbContext).Handle(
            new RefundPaymentCommand(payment.Id),
            "refund-key-1",
            CancellationToken.None);

        result.GetType().Name.Should().Contain("Conflict");
        (await dbContext.Set<IdempotencyRecord>().CountAsync()).Should().Be(0);
        (await dbContext.Payments.SingleAsync()).Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public async Task PaymentRefundRequestedConsumer_WhenPaymentSucceeded_RefundsAndOutboxesOnce()
    {
        await using var dbContext = CreateDbContext();
        var customerId = Guid.NewGuid();
        var payment = new ECommerce.Payment.Models.Payment(Guid.NewGuid(), 25m, "USD");
        payment.MarkSucceeded("provider-tx-1");
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync();

        var message = new ECommerce.Contracts.Payment.PaymentRefundRequestedIntegrationEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            payment.Id,
            payment.OrderId,
            customerId,
            "Order was already failed.");
        var consumer = new PaymentRefundRequestedConsumer(dbContext);

        await consumer.Consume(ConsumeContextFor(message));
        await consumer.Consume(ConsumeContextFor(message));

        (await dbContext.Payments.SingleAsync()).Status.Should().Be(PaymentStatus.Refunded);
        (await dbContext.Set<OutboxMessage>().CountAsync(outbox => outbox.Topic == KafkaTopics.PaymentRefunded)).Should().Be(1);
        (await dbContext.Set<InboxMessage>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task PaymentOutboxRepairService_WhenSucceededPaymentHasNoOutbox_RecreatesSuccessEvent()
    {
        await using var dbContext = CreateDbContext();
        var payment = new ECommerce.Payment.Models.Payment(Guid.NewGuid(), 25m, "USD");
        payment.MarkSucceeded("provider-tx-1");
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync();
        var services = new ServiceCollection();
        services.AddSingleton(dbContext);
        await using var provider = services.BuildServiceProvider();
        var repairService = new PaymentOutboxRepairService(
            provider,
            NullLogger<PaymentOutboxRepairService>.Instance);
        var method = typeof(PaymentOutboxRepairService)
            .GetMethod("RepairBatch", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("RepairBatch method was not found.");

        await (Task)method.Invoke(repairService, [CancellationToken.None])!;

        var outbox = await dbContext.Set<OutboxMessage>().SingleAsync();
        outbox.Topic.Should().Be(KafkaTopics.PaymentSucceeded);
        outbox.Payload.Should().Contain(payment.Id.ToString());
    }

    [Fact]
    public async Task StockReservedConsumer_WhenSameEventIsDeliveredTwice_CreatesSinglePaymentIntent()
    {
        await using var dbContext = CreateDbContext();
        var orderId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var message = new ECommerce.Contracts.Inventory.StockReservedIntegrationEvent(
            eventId,
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            orderId,
            Guid.NewGuid(),
            "buyer@example.com",
            25m,
            [new ECommerce.Contracts.Inventory.StockReservedLine("sku-1", 1)]);
        var consumer = new StockReservedConsumer(dbContext);

        await consumer.Consume(ConsumeContextFor(message));
        await consumer.Consume(ConsumeContextFor(message));

        var payment = await dbContext.Payments.SingleAsync();
        payment.OrderId.Should().Be(orderId);
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.ProviderIntentId.Should().StartWith("fake-intent-");
        (await dbContext.Set<InboxMessage>().CountAsync()).Should().Be(1);
    }

    private static PaymentDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PaymentDbContext(options);
    }

    private static ConsumeContext<T> ConsumeContextFor<T>(T message)
        where T : class
    {
        var context = new Mock<ConsumeContext<T>>();
        context.SetupGet(item => item.Message).Returns(message);
        context.SetupGet(item => item.CancellationToken).Returns(CancellationToken.None);
        return context.Object;
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
