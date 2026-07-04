using ECommerce.Inventory.Data;
using ECommerce.Inventory.Features.ReserveStock;
using ECommerce.Inventory.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Inventory.Tests;

public sealed class ReserveStockTests
{
    [Fact]
    public async Task ReserveStock_WhenAvailable_ReservesQuantityAndStoresReservation()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Items.Add(new InventoryItem("SKU-1", 10));
        await dbContext.SaveChangesAsync();
        var reservationId = Guid.NewGuid();

        await new ReserveStockHandler(dbContext).Handle(
            new ReserveStockCommand(reservationId, Guid.NewGuid(), [new ReserveStockLine("sku-1", 3)]),
            CancellationToken.None);

        var item = await dbContext.Items.SingleAsync();
        item.QuantityReserved.Should().Be(3);
        item.AvailableQuantity.Should().Be(7);
        (await dbContext.Reservations.CountAsync()).Should().Be(1);
        var movement = await dbContext.StockMovements.SingleAsync();
        movement.Type.Should().Be(StockMovementType.Reserved);
        movement.Quantity.Should().Be(3);
        movement.ReservationId.Should().Be(reservationId);
    }

    [Fact]
    public async Task ReserveStock_WhenOneLineIsInsufficient_DoesNotPartiallyReserveOtherLines()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Items.AddRange(
            new InventoryItem("SKU-OK", 10),
            new InventoryItem("SKU-LOW", 1));
        await dbContext.SaveChangesAsync();

        await new ReserveStockHandler(dbContext).Handle(
            new ReserveStockCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                [
                    new ReserveStockLine("sku-ok", 3),
                    new ReserveStockLine("sku-low", 2),
                ]),
            CancellationToken.None);

        var okItem = await dbContext.Items.SingleAsync(item => item.Sku == "SKU-OK");
        var lowItem = await dbContext.Items.SingleAsync(item => item.Sku == "SKU-LOW");
        okItem.QuantityReserved.Should().Be(0);
        lowItem.QuantityReserved.Should().Be(0);
        (await dbContext.Reservations.CountAsync()).Should().Be(0);
        (await dbContext.StockMovements.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ReserveStock_WhenSameReservationIsRetried_DoesNotReserveTwice()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Items.Add(new InventoryItem("SKU-1", 10));
        await dbContext.SaveChangesAsync();
        var reservationId = Guid.NewGuid();
        var command = new ReserveStockCommand(reservationId, Guid.NewGuid(), [new ReserveStockLine("sku-1", 3)]);
        var handler = new ReserveStockHandler(dbContext);

        await handler.Handle(command, CancellationToken.None);
        await handler.Handle(command, CancellationToken.None);

        var item = await dbContext.Items.SingleAsync();
        item.QuantityReserved.Should().Be(3);
        (await dbContext.Reservations.CountAsync()).Should().Be(1);
        (await dbContext.StockMovements.CountAsync()).Should().Be(1);
    }

    private static InventoryDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new InventoryDbContext(options);
    }
}
