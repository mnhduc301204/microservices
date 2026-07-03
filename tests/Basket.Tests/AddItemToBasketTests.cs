using ECommerce.Basket.Data;
using ECommerce.Basket.Features.AddItemToBasket;
using FluentAssertions;

namespace ECommerce.Basket.Tests;

public sealed class AddItemToBasketTests
{
    [Fact]
    public async Task AddItemToBasket_WhenSameSkuAddedTwice_IncreasesQuantity()
    {
        var basketStore = new InMemoryBasketStore();
        var customerId = Guid.NewGuid();
        var handler = new AddItemToBasketHandler(basketStore);

        await handler.Handle(new AddItemToBasketCommand(customerId, "sku-1", "Bottle", 12m, 1), CancellationToken.None);
        await handler.Handle(new AddItemToBasketCommand(customerId, "SKU-1", "Bottle", 12m, 2), CancellationToken.None);

        var item = (await basketStore.GetItems(customerId, CancellationToken.None)).Single();
        item.Quantity.Should().Be(3);
        item.Sku.Should().Be("SKU-1");
    }
}
