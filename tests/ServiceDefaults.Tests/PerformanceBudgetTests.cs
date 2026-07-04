using System.Diagnostics;
using ECommerce.Contracts.Ordering;
using ECommerce.ServiceDefaults.Messaging;
using FluentAssertions;
using Xunit;

namespace ECommerce.ServiceDefaults.Tests;

public sealed class PerformanceBudgetTests
{
    [Fact]
    public void OutboxMessageCreate_WithPartitionKeyResolution_StaysWithinBudget()
    {
        const int count = 100_000;
        var started = Stopwatch.StartNew();

        for (var index = 0; index < count; index++)
        {
            _ = OutboxMessage.Create(
                "ordering.order-created",
                new OrderCreatedIntegrationEvent(
                    Guid.NewGuid(),
                    DateTimeOffset.UtcNow,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "buyer@example.com",
                    25m,
                    [new OrderCreatedLine("SKU-1", "Bottle", 25m, 1)]));
        }

        started.Stop();

        started.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
    }
}
