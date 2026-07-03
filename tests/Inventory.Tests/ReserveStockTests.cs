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

    private static InventoryDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new InventoryDbContext(options);
    }
}
