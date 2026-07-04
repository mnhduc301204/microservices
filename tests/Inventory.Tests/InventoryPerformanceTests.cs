using System.Diagnostics;
using ECommerce.Inventory.Data;
using ECommerce.Inventory.Features.ReserveStock;
using ECommerce.Inventory.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Inventory.Tests;

public sealed class InventoryPerformanceTests
{
    [Fact]
    public async Task ReserveStock_WithManyLinesForSameSku_StaysWithinBudget()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Items.Add(new InventoryItem("SKU-1", 2_000));
        await dbContext.SaveChangesAsync();
        var command = new ReserveStockCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Enumerable.Range(0, 1_000)
                .Select(_ => new ReserveStockLine("sku-1", 1))
                .ToArray());
        var started = Stopwatch.StartNew();

        await new ReserveStockHandler(dbContext).Handle(command, CancellationToken.None);

        started.Stop();
        started.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
        var item = await dbContext.Items.SingleAsync();
        item.QuantityReserved.Should().Be(1_000);
        (await dbContext.Reservations.CountAsync()).Should().Be(1);
    }

    private static InventoryDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new InventoryDbContext(options);
    }
}
