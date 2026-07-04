using ECommerce.Basket.Features.AddItemToBasket;
using ECommerce.Basket.Features.CheckoutBasket;
using ECommerce.Basket.Features.ClearBasket;
using ECommerce.Basket.Features.RemoveItemFromBasket;
using FluentAssertions;

namespace ECommerce.Basket.Tests;

public sealed class BasketValidationMatrixTests
{
    [Theory]
    [MemberData(nameof(AddItemCommands))]
    public void AddItemToBasketValidator_ValidatesInput(AddItemToBasketCommand command, bool expected)
    {
        new AddItemToBasketValidator().Validate(command).IsValid.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(CheckoutCommands))]
    public void CheckoutBasketValidator_ValidatesInput(CheckoutBasketCommand command, bool expected)
    {
        new CheckoutBasketValidator().Validate(command).IsValid.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(RemoveItemCommands))]
    public void RemoveItemFromBasketValidator_ValidatesInput(RemoveItemFromBasketCommand command, bool expected)
    {
        new RemoveItemFromBasketValidator().Validate(command).IsValid.Should().Be(expected);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ClearBasketValidator_ValidatesCustomerId(bool validCustomerId)
    {
        var command = new ClearBasketCommand(validCustomerId ? Guid.NewGuid() : Guid.Empty);

        new ClearBasketValidator().Validate(command).IsValid.Should().Be(validCustomerId);
    }

    public static TheoryData<AddItemToBasketCommand, bool> AddItemCommands()
    {
        var valid = ValidAdd();
        var data = new TheoryData<AddItemToBasketCommand, bool>();
        data.Add(valid, true);
        data.Add(valid with { CustomerId = Guid.Empty }, false);
        data.Add(valid with { Sku = "" }, false);
        data.Add(valid with { Sku = " " }, false);
        data.Add(valid with { Sku = new string('S', 64) }, true);
        data.Add(valid with { Sku = new string('S', 65) }, false);
        data.Add(valid with { ProductName = "" }, false);
        data.Add(valid with { ProductName = " " }, false);
        data.Add(valid with { ProductName = new string('P', 200) }, true);
        data.Add(valid with { ProductName = new string('P', 201) }, false);
        data.Add(valid with { UnitPrice = 0m }, true);
        data.Add(valid with { UnitPrice = -0.01m }, false);
        data.Add(valid with { UnitPrice = -1m }, false);
        data.Add(valid with { Quantity = 1 }, true);
        data.Add(valid with { Quantity = 0 }, false);
        data.Add(valid with { Quantity = -1 }, false);
        data.Add(valid with { Quantity = int.MinValue }, false);
        return data;
    }

    public static TheoryData<CheckoutBasketCommand, bool> CheckoutCommands()
    {
        var valid = new CheckoutBasketCommand(Guid.NewGuid(), "buyer@example.com");
        var data = new TheoryData<CheckoutBasketCommand, bool>();
        data.Add(valid, true);
        data.Add(valid with { CustomerId = Guid.Empty }, false);
        data.Add(valid with { CustomerEmail = "" }, false);
        data.Add(valid with { CustomerEmail = " " }, false);
        data.Add(valid with { CustomerEmail = "not-email" }, false);
        data.Add(valid with { CustomerEmail = "buyer@localhost" }, true);
        data.Add(valid with { CustomerEmail = $"{new string('a', 314)}@x.com" }, true);
        data.Add(valid with { CustomerEmail = $"{new string('a', 315)}@x.com" }, false);
        return data;
    }

    public static TheoryData<RemoveItemFromBasketCommand, bool> RemoveItemCommands()
    {
        var valid = new RemoveItemFromBasketCommand(Guid.NewGuid(), "sku-1");
        var data = new TheoryData<RemoveItemFromBasketCommand, bool>();
        data.Add(valid, true);
        data.Add(valid with { CustomerId = Guid.Empty }, false);
        data.Add(valid with { Sku = "" }, false);
        data.Add(valid with { Sku = " " }, false);
        data.Add(valid with { Sku = "SKU-1" }, true);
        return data;
    }

    private static AddItemToBasketCommand ValidAdd() =>
        new(Guid.NewGuid(), "SKU-1", "Bottle", 12m, 1);
}
