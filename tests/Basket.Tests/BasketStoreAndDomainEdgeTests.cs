using ECommerce.Basket.Data;
using ECommerce.Basket.Models;
using FluentAssertions;

namespace ECommerce.Basket.Tests;

public sealed class BasketStoreAndDomainEdgeTests
{
    [Fact]
    public async Task InMemoryBasketStore_GetItems_ReturnsCopiesSoCallerCannotMutateStoredBasket()
    {
        var store = new InMemoryBasketStore();
        var customerId = Guid.NewGuid();
        await store.AddOrUpdateItem(customerId, "sku-1", "Bottle", 10m, 1, CancellationToken.None);

        var firstRead = await store.GetItems(customerId, CancellationToken.None);
        var mutatedCopy = firstRead.Single() with { Quantity = 999 };

        mutatedCopy.Quantity.Should().Be(999);
        var secondRead = await store.GetItems(customerId, CancellationToken.None);
        secondRead.Single().Quantity.Should().Be(1);
    }

    [Fact]
    public async Task InMemoryBasketStore_RemoveMissingItem_IsIdempotent()
    {
        var store = new InMemoryBasketStore();
        var customerId = Guid.NewGuid();
        await store.AddOrUpdateItem(customerId, "sku-1", "Bottle", 10m, 1, CancellationToken.None);

        await store.RemoveItem(customerId, "missing-sku", CancellationToken.None);
        await store.RemoveItem(customerId, "missing-sku", CancellationToken.None);

        (await store.GetItems(customerId, CancellationToken.None)).Should().ContainSingle();
    }

    [Fact]
    public async Task InMemoryBasketStore_ClearEmptyBasket_IsIdempotent()
    {
        var store = new InMemoryBasketStore();
        var customerId = Guid.NewGuid();

        await store.Clear(customerId, CancellationToken.None);
        await store.Clear(customerId, CancellationToken.None);

        (await store.GetItems(customerId, CancellationToken.None)).Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void BasketItem_WhenSkuIsBlank_Throws(string sku)
    {
        Action act = () => _ = new BasketItem(Guid.NewGuid(), sku, "Bottle", 10m, 1);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1)]
    public void BasketItem_WhenUnitPriceIsNegative_Throws(decimal unitPrice)
    {
        Action act = () => _ = new BasketItem(Guid.NewGuid(), "sku-1", "Bottle", unitPrice, 1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
