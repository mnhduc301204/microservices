using ECommerce.Inventory.Features.DeductStock;
using ECommerce.Inventory.Features.ReleaseReservation;
using ECommerce.Inventory.Features.ReserveStock;
using ECommerce.Inventory.Models;
using FluentAssertions;

namespace ECommerce.Inventory.Tests;

public sealed class InventoryValidationAndDomainMatrixTests
{
    [Theory]
    [MemberData(nameof(ReserveStockCommands))]
    public void ReserveStockValidator_ValidatesInput(ReserveStockCommand command, bool expected)
    {
        new ReserveStockValidator().Validate(command).IsValid.Should().Be(expected);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReleaseReservationValidator_ValidatesReservationId(bool validReservationId)
    {
        var command = new ReleaseReservationCommand(validReservationId ? Guid.NewGuid() : Guid.Empty);

        new ReleaseReservationValidator().Validate(command).IsValid.Should().Be(validReservationId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DeductStockValidator_ValidatesReservationId(bool validReservationId)
    {
        var command = new DeductStockCommand(validReservationId ? Guid.NewGuid() : Guid.Empty);

        new DeductStockValidator().Validate(command).IsValid.Should().Be(validReservationId);
    }

    [Theory]
    [InlineData("sku-1", "SKU-1")]
    [InlineData(" SKU-1 ", "SKU-1")]
    [InlineData("abc-123", "ABC-123")]
    [InlineData(" MiXeD ", "MIXED")]
    public void InventoryItem_NormalizesSku(string input, string expected)
    {
        new InventoryItem(input, 1).Sku.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void InventoryItem_WhenSkuIsBlank_Throws(string sku)
    {
        Action act = () => _ = new InventoryItem(sku, 1);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(10, 1, true, 1, 9)]
    [InlineData(10, 10, true, 10, 0)]
    [InlineData(10, 11, false, 0, 10)]
    [InlineData(10, 0, false, 0, 10)]
    [InlineData(10, -1, false, 0, 10)]
    public void InventoryItem_TryReserve_RespectsAvailableQuantity(int onHand, int reserve, bool expected, int expectedReserved, int expectedAvailable)
    {
        var item = new InventoryItem("sku-1", onHand);

        item.TryReserve(reserve).Should().Be(expected);

        item.QuantityReserved.Should().Be(expectedReserved);
        item.AvailableQuantity.Should().Be(expectedAvailable);
    }

    [Theory]
    [InlineData(5, 3, 2)]
    [InlineData(5, 10, 0)]
    public void InventoryItem_Release_NeverMakesReservedNegative(int initiallyReserved, int release, int expectedReserved)
    {
        var item = new InventoryItem("sku-1", 10);
        item.TryReserve(initiallyReserved);

        item.Release(release);

        item.QuantityReserved.Should().Be(expectedReserved);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InventoryItem_Release_WhenQuantityIsNotPositive_Throws(int quantity)
    {
        var item = new InventoryItem("sku-1", 10);

        Action act = () => item.Release(quantity);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(5, 3, 7, 2)]
    [InlineData(5, 5, 5, 0)]
    public void InventoryItem_DeductReserved_DecreasesOnHandAndReserved(int reserved, int deduct, int expectedOnHand, int expectedReserved)
    {
        var item = new InventoryItem("sku-1", 10);
        item.TryReserve(reserved);

        item.DeductReserved(deduct);

        item.QuantityOnHand.Should().Be(expectedOnHand);
        item.QuantityReserved.Should().Be(expectedReserved);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    public void InventoryItem_DeductReserved_WhenInvalid_Throws(int deduct)
    {
        var item = new InventoryItem("sku-1", 10);
        item.TryReserve(5);

        Action act = () => item.DeductReserved(deduct);

        act.Should().Throw<Exception>();
    }

    public static TheoryData<ReserveStockCommand, bool> ReserveStockCommands()
    {
        var valid = new ReserveStockCommand(Guid.NewGuid(), Guid.NewGuid(), [new ReserveStockLine("sku-1", 1)]);
        var data = new TheoryData<ReserveStockCommand, bool>();
        data.Add(valid, true);
        data.Add(valid with { ReservationId = Guid.Empty }, false);
        data.Add(valid with { OrderId = null }, true);
        data.Add(valid with { Lines = [] }, false);
        data.Add(valid with { Lines = [new ReserveStockLine("", 1)] }, false);
        data.Add(valid with { Lines = [new ReserveStockLine(" ", 1)] }, false);
        data.Add(valid with { Lines = [new ReserveStockLine(new string('S', 64), 1)] }, true);
        data.Add(valid with { Lines = [new ReserveStockLine(new string('S', 65), 1)] }, false);
        data.Add(valid with { Lines = [new ReserveStockLine("sku-1", 0)] }, false);
        data.Add(valid with { Lines = [new ReserveStockLine("sku-1", -1)] }, false);
        data.Add(valid with { Lines = [new ReserveStockLine("sku-1", 1), new ReserveStockLine("sku-2", 2)] }, true);
        return data;
    }
}
