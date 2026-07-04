using ECommerce.Contracts.Inventory;
using ECommerce.Contracts.Ordering;
using ECommerce.ServiceDefaults.Messaging;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ECommerce.ServiceDefaults.Tests;

public sealed class OutboxMessageTests
{
    [Fact]
    public void Create_UsesOrderIdAsPartitionKey_WhenEventHasOrderId()
    {
        var orderId = Guid.NewGuid();
        var message = OutboxMessage.Create(
            "ordering.order-created",
            new OrderCreatedIntegrationEvent(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                orderId,
                Guid.NewGuid(),
                "buyer@example.com",
                25m,
                [new OrderCreatedLine("SKU-1", "Bottle", 25m, 1)]));

        message.PartitionKey.Should().Be(orderId.ToString());
    }

    [Fact]
    public void Create_UsesOrderIdBeforeReservationId_ForOrderRelatedEvents()
    {
        var reservationId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var message = OutboxMessage.Create(
            "inventory.release-stock-reservation",
            new ReleaseStockReservationIntegrationEvent(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                reservationId,
                orderId,
                "test"));

        message.PartitionKey.Should().Be(orderId.ToString());
    }

    [Fact]
    public void Create_FallsBackToEventId_WhenNoAggregateKeyExists()
    {
        var eventId = Guid.NewGuid();
        var message = OutboxMessage.Create("test.event", new TestOnlyIntegrationEvent(eventId, DateTimeOffset.UtcNow));

        message.PartitionKey.Should().Be(eventId.ToString());
    }

    [Fact]
    public void MarkFailed_AppliesBackoffAndDeadLettersAfterTenAttempts()
    {
        var message = OutboxMessage.Create(
            "ordering.order-created",
            new OrderCreatedIntegrationEvent(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "buyer@example.com",
                25m,
                []));

        for (var attempt = 0; attempt < 9; attempt++)
        {
            message.MarkFailed(new InvalidOperationException("publish failed"));
            message.IsDeadLetter.Should().BeFalse();
            message.NextAttemptAt.Should().NotBeNull();
        }

        message.MarkFailed(new InvalidOperationException("publish failed"));

        message.IsDeadLetter.Should().BeTrue();
        message.NextAttemptAt.Should().BeNull();
    }
}

public sealed class InboxExtensionsTests
{
    [Fact]
    public async Task TryBeginProcessing_ReturnsFalse_AfterMessageMarkedProcessed()
    {
        await using var dbContext = CreateDbContext();
        var eventId = Guid.NewGuid();
        const string consumer = "test-consumer";

        var firstClaim = await dbContext.TryBeginProcessingAsync(eventId, consumer, CancellationToken.None);
        dbContext.MarkProcessed(eventId, consumer);
        await dbContext.SaveChangesAsync();
        var secondClaim = await dbContext.TryBeginProcessingAsync(eventId, consumer, CancellationToken.None);

        firstClaim.Should().BeTrue();
        secondClaim.Should().BeFalse();
        (await dbContext.Set<InboxMessage>().CountAsync()).Should().Be(1);
    }

    private static ReliabilityTestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ReliabilityTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ReliabilityTestDbContext(options);
    }
}

public sealed class ReliabilityTestDbContext(DbContextOptions<ReliabilityTestDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddInboxOutboxEntities();
    }
}

public sealed record TestOnlyIntegrationEvent(Guid EventId, DateTimeOffset OccurredAt);
