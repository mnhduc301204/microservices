using System.Diagnostics;
using ECommerce.Basket.Data;
using FluentAssertions;

namespace ECommerce.Basket.Tests;

public sealed class BasketPerformanceTests
{
    [Fact]
    public async Task InMemoryBasketStore_AddOrUpdateSameSku_StaysWithinBudget()
    {
        var store = new InMemoryBasketStore();
        var customerId = Guid.NewGuid();
        var started = Stopwatch.StartNew();

        for (var index = 0; index < 10_000; index++)
        {
            await store.AddOrUpdateItem(customerId, "sku-1", "Bottle", 12m, 1, CancellationToken.None);
        }

        started.Stop();

        started.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
        var item = (await store.GetItems(customerId, CancellationToken.None)).Single();
        item.Quantity.Should().Be(10_000);
    }
}
