using ECommerce.Contracts;
using ECommerce.Contracts.Inventory;
using ECommerce.Contracts.Ordering;
using ECommerce.Contracts.Payment;
using ECommerce.Catalog.Data;
using ECommerce.Catalog.Models;
using ECommerce.Inventory.Data;
using ECommerce.Inventory.Consumers;
using ECommerce.Inventory.Models;
using ECommerce.Notification.Consumers;
using ECommerce.Notification.Data;
using ECommerce.Ordering.Consumers;
using ECommerce.Ordering.Data;
using ECommerce.Ordering.Models;
using ECommerce.Payment.Data;
using ECommerce.Payment.Models;
using ECommerce.ServiceDefaults.Messaging;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Reflection;
using Testcontainers.PostgreSql;
using Xunit;

namespace ECommerce.Infrastructure.Tests;

public sealed class ReliabilityPostgresTests
{
    [Fact]
    public async Task InventoryConditionalReserve_AllowsOnlyAvailableQuantity_UnderConcurrentClaims()
    {
        await using var postgres = await TryStartPostgres();
        if (postgres is null)
        {
            return;
        }

        var options = InventoryOptions(postgres.GetConnectionString());
        await using (var setup = new InventoryDbContext(options))
        {
            await setup.Database.MigrateAsync();
            setup.Items.Add(new InventoryItem("SKU-HOT", 1));
            await setup.SaveChangesAsync();
        }

        var reserveAttempts = Enumerable.Range(0, 8).Select(async _ =>
        {
            await using var dbContext = new InventoryDbContext(options);
            return await dbContext.Items
                .Where(item => item.Sku == "SKU-HOT" && item.QuantityOnHand - item.QuantityReserved >= 1)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.QuantityReserved, item => item.QuantityReserved + 1));
        });

        var results = await Task.WhenAll(reserveAttempts);

        results.Sum().Should().Be(1);
        await using var verify = new InventoryDbContext(options);
        var item = await verify.Items.SingleAsync(item => item.Sku == "SKU-HOT");
        item.QuantityReserved.Should().Be(1);
        item.AvailableQuantity.Should().Be(0);
    }

    [Fact]
    public async Task PaymentProviderIntentClaim_AllowsOnlyOneWorker_ToClaimPendingPayment()
    {
        await using var postgres = await TryStartPostgres();
        if (postgres is null)
        {
            return;
        }

        var options = PaymentOptions(postgres.GetConnectionString());
        Guid paymentId;
        await using (var setup = new PaymentDbContext(options))
        {
            await setup.Database.MigrateAsync();
            var newPayment = new ECommerce.Payment.Models.Payment(Guid.NewGuid(), 10m, "USD");
            newPayment.RequestProviderIntent("fake-intent");
            setup.Payments.Add(newPayment);
            await setup.SaveChangesAsync();
            paymentId = newPayment.Id;
        }

        var lockCutoff = DateTimeOffset.UtcNow.AddMinutes(-5);
        var claimAttempts = Enumerable.Range(0, 8).Select(async worker =>
        {
            await using var dbContext = new PaymentDbContext(options);
            var workerId = $"worker-{worker}";
            return await dbContext.Payments
                .Where(payment =>
                    payment.Id == paymentId
                    && payment.Status == PaymentStatus.Pending
                    && payment.ProviderIntentId != null
                    && (payment.ProviderLockedAt == null || payment.ProviderLockedAt <= lockCutoff))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(payment => payment.ProviderLockedAt, DateTimeOffset.UtcNow)
                    .SetProperty(payment => payment.ProviderLockedBy, workerId));
        });

        var results = await Task.WhenAll(claimAttempts);

        results.Sum().Should().Be(1);
        await using var verify = new PaymentDbContext(options);
        var payment = await verify.Payments.SingleAsync(payment => payment.Id == paymentId);
        payment.ProviderLockedBy.Should().StartWith("worker-");
    }

    [Fact]
    public async Task InventoryOrderCreatedConsumer_WhenLaterLineCannotReserve_RollsBackEarlierReservationAndPublishesFailure()
    {
        await using var postgres = await TryStartPostgres();
        if (postgres is null)
        {
            return;
        }

        var options = InventoryOptions(postgres.GetConnectionString());
        await using (var setup = new InventoryDbContext(options))
        {
            await setup.Database.MigrateAsync();
            setup.Items.AddRange(
                new InventoryItem("SKU-OK", 10),
                new InventoryItem("SKU-LOW", 1));
            await setup.SaveChangesAsync();
        }

        var eventId = Guid.NewGuid();
        await using (var dbContext = new InventoryDbContext(options))
        {
            var message = new OrderCreatedIntegrationEvent(
                eventId,
                DateTimeOffset.UtcNow,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "buyer@example.com",
                50m,
                [
                    new OrderCreatedLine("sku-ok", "Available product", 10m, 2),
                    new OrderCreatedLine("sku-low", "Short product", 10m, 2),
                ]);

            await new OrderCreatedConsumer(dbContext).Consume(ConsumeContextFor(message));
        }

        await using var verify = new InventoryDbContext(options);
        var okItem = await verify.Items.SingleAsync(item => item.Sku == "SKU-OK");
        var lowItem = await verify.Items.SingleAsync(item => item.Sku == "SKU-LOW");
        okItem.QuantityReserved.Should().Be(0);
        lowItem.QuantityReserved.Should().Be(0);
        (await verify.Reservations.CountAsync()).Should().Be(0);
        (await verify.StockMovements.CountAsync()).Should().Be(0);

        var failure = await verify.Set<OutboxMessage>().SingleAsync();
        failure.Topic.Should().Be(KafkaTopics.StockReservationFailed);
        failure.MessageType.Should().Contain("StockReservationFailedIntegrationEvent");
        (await verify.Set<InboxMessage>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task OrderingPaymentFailedConsumer_WhenStockWasReserved_FailsOrderAndPublishesCancelAndReleaseOnce()
    {
        await using var postgres = await TryStartPostgres();
        if (postgres is null)
        {
            return;
        }

        var options = OrderingOptions(postgres.GetConnectionString());
        Guid orderId;
        Guid customerId;
        var reservationId = Guid.NewGuid();
        await using (var setup = new OrderingDbContext(options))
        {
            await setup.Database.MigrateAsync();
            customerId = Guid.NewGuid();
            var newOrder = new Order(customerId, "buyer@example.com", [new OrderItemDraft("sku-1", "Bottle", 10m, 1)]);
            var saga = new CheckoutSagaState(Guid.NewGuid(), newOrder.Id, customerId);
            saga.MarkStockReserved(reservationId);
            setup.Orders.Add(newOrder);
            setup.CheckoutSagas.Add(saga);
            await setup.SaveChangesAsync();
            orderId = newOrder.Id;
        }

        var failedEvent = new PaymentFailedIntegrationEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            orderId,
            customerId,
            "Provider timed out after stock was reserved.");

        await using (var dbContext = new OrderingDbContext(options))
        {
            var consumer = new PaymentFailedConsumer(dbContext);
            await consumer.Consume(ConsumeContextFor(failedEvent));
            await consumer.Consume(ConsumeContextFor(failedEvent));
        }

        await using var verify = new OrderingDbContext(options);
        var order = await verify.Orders.SingleAsync(order => order.Id == orderId);
        order.Status.Should().Be(OrderStatus.Failed);

        var sagaState = await verify.CheckoutSagas.SingleAsync(saga => saga.OrderId == orderId);
        sagaState.Status.Should().Be(CheckoutSagaStatus.Failed);
        sagaState.StockReservationId.Should().Be(reservationId);

        var outboxMessages = await verify.Set<OutboxMessage>().OrderBy(message => message.Topic).ToListAsync();
        outboxMessages.Should().HaveCount(2);
        outboxMessages.Select(message => message.Topic).Should().BeEquivalentTo(
            KafkaTopics.OrderCancelled,
            KafkaTopics.ReleaseStockReservation);
        (await verify.Set<InboxMessage>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Inbox_WhenMessageIsLockedByActiveWorker_DoesNotAllowSecondClaim()
    {
        await using var postgres = await TryStartPostgres();
        if (postgres is null)
        {
            return;
        }

        var connectionString = postgres.GetConnectionString();
        var options = MessagingOptions(connectionString);
        await using (var setup = new MessagingReliabilityDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
        }

        var eventId = Guid.NewGuid();
        await using var first = new MessagingReliabilityDbContext(options);
        await using var second = new MessagingReliabilityDbContext(options);

        (await first.TryBeginProcessingAsync(eventId, "ConsumerA", CancellationToken.None)).Should().BeTrue();
        (await second.TryBeginProcessingAsync(eventId, "ConsumerA", CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task Inbox_WhenProcessingLockIsStale_AllowsAnotherWorkerToClaim()
    {
        await using var postgres = await TryStartPostgres();
        if (postgres is null)
        {
            return;
        }

        var connectionString = postgres.GetConnectionString();
        var options = MessagingOptions(connectionString);
        await using (var setup = new MessagingReliabilityDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
        }

        var eventId = Guid.NewGuid();
        await using (var first = new MessagingReliabilityDbContext(options))
        {
            (await first.TryBeginProcessingAsync(eventId, "ConsumerA", CancellationToken.None)).Should().BeTrue();
        }

        await using (var stale = new MessagingReliabilityDbContext(options))
        {
            await stale.Set<InboxMessage>()
                .Where(message => message.EventId == eventId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(message => message.LockedAt, DateTimeOffset.UtcNow.AddMinutes(-10)));
        }

        await using var second = new MessagingReliabilityDbContext(options);
        (await second.TryBeginProcessingAsync(eventId, "ConsumerA", CancellationToken.None)).Should().BeTrue();
    }

    [Fact]
    public async Task OutboxDispatcher_WhenProducerSucceeds_MarksMessageProcessed()
    {
        await using var postgres = await TryStartPostgres();
        if (postgres is null)
        {
            return;
        }

        var connectionString = postgres.GetConnectionString();
        var options = MessagingOptions(connectionString);
        var orderCreated = new OrderCreatedIntegrationEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "buyer@example.com",
            10m,
            [new OrderCreatedLine("sku-1", "Bottle", 10m, 1)]);
        await using (var setup = new MessagingReliabilityDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Set<OutboxMessage>().Add(OutboxMessage.Create(KafkaTopics.OrderCreated, orderCreated));
            await setup.SaveChangesAsync();
        }

        var producer = new Mock<ITopicProducer<string, OrderCreatedIntegrationEvent>>();
        producer
            .Setup(item => item.Produce(orderCreated.OrderId.ToString(), It.IsAny<OrderCreatedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        await DispatchOutbox(connectionString, services => services.AddSingleton(producer.Object));

        await using var verify = new MessagingReliabilityDbContext(options);
        var message = await verify.Set<OutboxMessage>().SingleAsync();
        message.ProcessedAt.Should().NotBeNull();
        message.AttemptCount.Should().Be(0);
        producer.Verify(
            item => item.Produce(orderCreated.OrderId.ToString(), It.IsAny<OrderCreatedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OutboxDispatcher_WhenProducerIsMissing_LeavesMessagePendingAndSchedulesRetry()
    {
        await using var postgres = await TryStartPostgres();
        if (postgres is null)
        {
            return;
        }

        var connectionString = postgres.GetConnectionString();
        var options = MessagingOptions(connectionString);
        await using (var setup = new MessagingReliabilityDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Set<OutboxMessage>().Add(OutboxMessage.Create(
                KafkaTopics.OrderCreated,
                new OrderCreatedIntegrationEvent(
                    Guid.NewGuid(),
                    DateTimeOffset.UtcNow,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "buyer@example.com",
                    10m,
                    [new OrderCreatedLine("sku-1", "Bottle", 10m, 1)])));
            await setup.SaveChangesAsync();
        }

        await DispatchOutbox(connectionString, _ => { });

        await using var verify = new MessagingReliabilityDbContext(options);
        var message = await verify.Set<OutboxMessage>().SingleAsync();
        message.ProcessedAt.Should().BeNull();
        message.AttemptCount.Should().Be(1);
        message.NextAttemptAt.Should().NotBeNull();
        message.IsDeadLetter.Should().BeFalse();
    }

    [Fact]
    public async Task InventoryReleaseReservationConsumer_WhenSameReleaseIsDeliveredTwice_ReleasesStockOnce()
    {
        await using var postgres = await TryStartPostgres();
        if (postgres is null)
        {
            return;
        }

        var options = InventoryOptions(postgres.GetConnectionString());
        var reservationId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        await using (var setup = new InventoryDbContext(options))
        {
            await setup.Database.MigrateAsync();
            var item = new InventoryItem("SKU-1", 10);
            item.TryReserve(4);
            setup.Items.Add(item);
            setup.Reservations.Add(new StockReservation(reservationId, "SKU-1", 4, orderId));
            await setup.SaveChangesAsync();
        }

        var release = new ReleaseStockReservationIntegrationEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            reservationId,
            orderId,
            "Payment failed.");
        await using (var dbContext = new InventoryDbContext(options))
        {
            var consumer = new ReleaseStockReservationConsumer(dbContext);
            await consumer.Consume(ConsumeContextFor(release));
            await consumer.Consume(ConsumeContextFor(release));
        }

        await using var verify = new InventoryDbContext(options);
        var itemAfterRelease = await verify.Items.SingleAsync();
        itemAfterRelease.QuantityReserved.Should().Be(0);
        (await verify.StockMovements.CountAsync(movement => movement.Type == StockMovementType.Released)).Should().Be(1);
        (await verify.Set<InboxMessage>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task InventoryOrderConfirmedConsumer_WhenSameConfirmationIsDeliveredTwice_DeductsReservedStockOnce()
    {
        await using var postgres = await TryStartPostgres();
        if (postgres is null)
        {
            return;
        }

        var options = InventoryOptions(postgres.GetConnectionString());
        var reservationId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        await using (var setup = new InventoryDbContext(options))
        {
            await setup.Database.MigrateAsync();
            var item = new InventoryItem("SKU-1", 10);
            item.TryReserve(4);
            setup.Items.Add(item);
            setup.Reservations.Add(new StockReservation(reservationId, "SKU-1", 4, orderId));
            await setup.SaveChangesAsync();
        }

        var confirmed = new OrderConfirmedIntegrationEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            orderId,
            customerId,
            40m);
        await using (var dbContext = new InventoryDbContext(options))
        {
            var consumer = new ECommerce.Inventory.Consumers.OrderConfirmedConsumer(dbContext);
            await consumer.Consume(ConsumeContextFor(confirmed));
            await consumer.Consume(ConsumeContextFor(confirmed));
        }

        await using var verify = new InventoryDbContext(options);
        var itemAfterDeduct = await verify.Items.SingleAsync();
        itemAfterDeduct.QuantityOnHand.Should().Be(6);
        itemAfterDeduct.QuantityReserved.Should().Be(0);

        var reservation = await verify.Reservations.SingleAsync();
        reservation.Status.Should().Be(StockReservationStatus.Deducted);
        reservation.CompletedAt.Should().NotBeNull();
        (await verify.StockMovements.CountAsync(movement => movement.Type == StockMovementType.Deducted)).Should().Be(1);
        (await verify.Set<InboxMessage>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task OrderingPaymentSucceededConsumer_WhenSameEventIsDeliveredTwice_ConfirmsAndOutboxesOnce()
    {
        await using var postgres = await TryStartPostgres();
        if (postgres is null)
        {
            return;
        }

        var options = OrderingOptions(postgres.GetConnectionString());
        var customerId = Guid.NewGuid();
        Guid orderId;
        await using (var setup = new OrderingDbContext(options))
        {
            await setup.Database.MigrateAsync();
            var order = new Order(customerId, "buyer@example.com", [new OrderItemDraft("sku-1", "Bottle", 10m, 1)]);
            setup.Orders.Add(order);
            setup.CheckoutSagas.Add(new CheckoutSagaState(Guid.NewGuid(), order.Id, customerId));
            await setup.SaveChangesAsync();
            orderId = order.Id;
        }

        var paymentSucceeded = new PaymentSucceededIntegrationEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            orderId,
            customerId,
            "provider-tx",
            10m);
        await using (var dbContext = new OrderingDbContext(options))
        {
            var consumer = new PaymentSucceededConsumer(dbContext);
            await consumer.Consume(ConsumeContextFor(paymentSucceeded));
            await consumer.Consume(ConsumeContextFor(paymentSucceeded));
        }

        await using var verify = new OrderingDbContext(options);
        (await verify.Orders.SingleAsync()).Status.Should().Be(OrderStatus.Confirmed);
        (await verify.Set<OutboxMessage>().CountAsync(message => message.Topic == KafkaTopics.OrderConfirmed)).Should().Be(1);
        (await verify.Set<InboxMessage>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task OrderingPaymentSucceededConsumer_WhenOrderAlreadyFailed_DoesNotReopenOrderAndRequestsRefund()
    {
        await using var postgres = await TryStartPostgres();
        if (postgres is null)
        {
            return;
        }

        var options = OrderingOptions(postgres.GetConnectionString());
        var customerId = Guid.NewGuid();
        Guid orderId;
        await using (var setup = new OrderingDbContext(options))
        {
            await setup.Database.MigrateAsync();
            var order = new Order(customerId, "buyer@example.com", [new OrderItemDraft("sku-1", "Bottle", 10m, 1)]);
            order.Fail();
            setup.Orders.Add(order);
            setup.CheckoutSagas.Add(new CheckoutSagaState(Guid.NewGuid(), order.Id, customerId));
            await setup.SaveChangesAsync();
            orderId = order.Id;
        }

        await using (var dbContext = new OrderingDbContext(options))
        {
            await new PaymentSucceededConsumer(dbContext).Consume(ConsumeContextFor(new PaymentSucceededIntegrationEvent(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                Guid.NewGuid(),
                orderId,
                customerId,
                "provider-tx",
                10m)));
        }

        await using var verify = new OrderingDbContext(options);
        (await verify.Orders.SingleAsync()).Status.Should().Be(OrderStatus.Failed);
        (await verify.Set<OutboxMessage>().CountAsync(message => message.Topic == KafkaTopics.OrderConfirmed)).Should().Be(0);
        (await verify.Set<OutboxMessage>().CountAsync(message => message.Topic == KafkaTopics.PaymentRefundRequested)).Should().Be(1);
    }

    [Fact]
    public async Task OrderingStockReservationFailedConsumer_WhenSameEventIsDeliveredTwice_CancelsOnce()
    {
        await using var postgres = await TryStartPostgres();
        if (postgres is null)
        {
            return;
        }

        var options = OrderingOptions(postgres.GetConnectionString());
        var customerId = Guid.NewGuid();
        Guid orderId;
        await using (var setup = new OrderingDbContext(options))
        {
            await setup.Database.MigrateAsync();
            var order = new Order(customerId, "buyer@example.com", [new OrderItemDraft("sku-1", "Bottle", 10m, 1)]);
            setup.Orders.Add(order);
            setup.CheckoutSagas.Add(new CheckoutSagaState(Guid.NewGuid(), order.Id, customerId));
            await setup.SaveChangesAsync();
            orderId = order.Id;
        }

        var stockFailed = new StockReservationFailedIntegrationEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            orderId,
            customerId,
            "Insufficient stock.");
        await using (var dbContext = new OrderingDbContext(options))
        {
            var consumer = new StockReservationFailedConsumer(dbContext);
            await consumer.Consume(ConsumeContextFor(stockFailed));
            await consumer.Consume(ConsumeContextFor(stockFailed));
        }

        await using var verify = new OrderingDbContext(options);
        (await verify.Orders.SingleAsync()).Status.Should().Be(OrderStatus.Failed);
        (await verify.Set<OutboxMessage>().CountAsync(message => message.Topic == KafkaTopics.OrderCancelled)).Should().Be(1);
        (await verify.Set<InboxMessage>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task NotificationConsumer_WhenSameEventIsDeliveredTwice_RecordsNotificationOnce()
    {
        await using var postgres = await TryStartPostgres();
        if (postgres is null)
        {
            return;
        }

        var options = NotificationOptions(postgres.GetConnectionString());
        await using (var setup = new NotificationDbContext(options))
        {
            await setup.Database.MigrateAsync();
        }

        var orderCreated = new OrderCreatedIntegrationEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "buyer@example.com",
            10m,
            [new OrderCreatedLine("sku-1", "Bottle", 10m, 1)]);
        await using (var dbContext = new NotificationDbContext(options))
        {
            var consumer = new OrderCreatedNotificationConsumer(dbContext);
            await consumer.Consume(ConsumeContextFor(orderCreated));
            await consumer.Consume(ConsumeContextFor(orderCreated));
        }

        await using var verify = new NotificationDbContext(options);
        (await verify.Notifications.CountAsync()).Should().Be(1);
        (await verify.Set<InboxMessage>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CatalogDatabase_WhenDuplicateProductSkuIsInserted_RollsBackDuplicateAndPreservesOriginal()
    {
        await using var postgres = await TryStartPostgres();
        if (postgres is null)
        {
            return;
        }

        var options = CatalogOptions(postgres.GetConnectionString());
        await using (var setup = new CatalogDbContext(options))
        {
            await setup.Database.MigrateAsync();
            setup.Products.Add(new Product("Bottle", "sku-1", 10m, Guid.NewGuid(), Guid.NewGuid(), null));
            await setup.SaveChangesAsync();
        }

        await using (var dbContext = new CatalogDbContext(options))
        {
            dbContext.Products.Add(new Product("Duplicate Bottle", "SKU-1", 12m, Guid.NewGuid(), Guid.NewGuid(), null));
            Func<Task> act = async () => await dbContext.SaveChangesAsync();

            await act.Should().ThrowAsync<DbUpdateException>();
        }

        await using var verify = new CatalogDbContext(options);
        (await verify.Products.CountAsync()).Should().Be(1);
        (await verify.Products.SingleAsync()).Name.Should().Be("Bottle");
    }

    [Fact]
    public async Task PaymentDatabase_WhenDuplicateProviderTransactionIsInserted_RejectsDuplicate()
    {
        await using var postgres = await TryStartPostgres();
        if (postgres is null)
        {
            return;
        }

        var options = PaymentOptions(postgres.GetConnectionString());
        await using (var setup = new PaymentDbContext(options))
        {
            await setup.Database.MigrateAsync();
            var first = new ECommerce.Payment.Models.Payment(Guid.NewGuid(), 10m, "USD");
            first.MarkSucceeded("provider-tx-1");
            setup.Payments.Add(first);
            await setup.SaveChangesAsync();
        }

        await using (var dbContext = new PaymentDbContext(options))
        {
            var duplicate = new ECommerce.Payment.Models.Payment(Guid.NewGuid(), 20m, "USD");
            duplicate.MarkSucceeded("provider-tx-1");
            dbContext.Payments.Add(duplicate);
            Func<Task> act = async () => await dbContext.SaveChangesAsync();

            await act.Should().ThrowAsync<DbUpdateException>();
        }

        await using var verify = new PaymentDbContext(options);
        (await verify.Payments.CountAsync()).Should().Be(1);
    }

    private static async Task<PostgreSqlContainer?> TryStartPostgres()
    {
        try
        {
            var postgres = new PostgreSqlBuilder()
                .WithImage("postgres:17-alpine")
                .WithDatabase("reliability_test")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await postgres.StartAsync();
            return postgres;
        }
        catch (Exception ex) when (ex.Message.Contains("Docker", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
    }

    private static DbContextOptions<InventoryDbContext> InventoryOptions(string connectionString) =>
        new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(connectionString)
            .Options;

    private static DbContextOptions<CatalogDbContext> CatalogOptions(string connectionString) =>
        new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(connectionString)
            .Options;

    private static DbContextOptions<PaymentDbContext> PaymentOptions(string connectionString) =>
        new DbContextOptionsBuilder<PaymentDbContext>()
            .UseNpgsql(connectionString)
            .Options;

    private static DbContextOptions<OrderingDbContext> OrderingOptions(string connectionString) =>
        new DbContextOptionsBuilder<OrderingDbContext>()
            .UseNpgsql(connectionString)
            .Options;

    private static DbContextOptions<NotificationDbContext> NotificationOptions(string connectionString) =>
        new DbContextOptionsBuilder<NotificationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

    private static DbContextOptions<MessagingReliabilityDbContext> MessagingOptions(string connectionString) =>
        new DbContextOptionsBuilder<MessagingReliabilityDbContext>()
            .UseNpgsql(connectionString)
            .Options;

    private static ConsumeContext<T> ConsumeContextFor<T>(T message)
        where T : class
    {
        var context = new Mock<ConsumeContext<T>>();
        context.SetupGet(item => item.Message).Returns(message);
        context.SetupGet(item => item.CancellationToken).Returns(CancellationToken.None);
        return context.Object;
    }

    private static async Task DispatchOutbox(
        string connectionString,
        Action<IServiceCollection> configureServices)
    {
        var services = new ServiceCollection();
        services.AddDbContext<MessagingReliabilityDbContext>(db => db.UseNpgsql(connectionString));
        services.AddScoped<KafkaOutboxPublisher>();
        configureServices(services);
        await using var provider = services.BuildServiceProvider();
        var dispatcher = new OutboxDispatcher<MessagingReliabilityDbContext>(
            provider,
            NullLogger<OutboxDispatcher<MessagingReliabilityDbContext>>.Instance);
        var method = typeof(OutboxDispatcher<MessagingReliabilityDbContext>)
            .GetMethod("DispatchBatch", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("DispatchBatch method was not found.");

        await (Task)method.Invoke(dispatcher, [CancellationToken.None])!;
    }

    private sealed class MessagingReliabilityDbContext(DbContextOptions<MessagingReliabilityDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddInboxOutboxEntities();
        }
    }
}
