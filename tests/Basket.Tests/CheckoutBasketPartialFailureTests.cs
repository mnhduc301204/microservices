using ECommerce.Basket.Data;
using ECommerce.Basket.Features.AddItemToBasket;
using ECommerce.Basket.Features.CheckoutBasket;
using ECommerce.Contracts.Basket;
using FluentAssertions;
using MassTransit;
using Moq;

namespace ECommerce.Basket.Tests;

public sealed class CheckoutBasketPartialFailureTests
{
    [Fact]
    public async Task CheckoutBasket_WhenPublishFails_DoesNotClearBasket()
    {
        var basketStore = new InMemoryBasketStore();
        var customerId = Guid.NewGuid();
        await new AddItemToBasketHandler(basketStore).Execute(
            new AddItemToBasketCommand(customerId, "sku-1", "Bottle", 12m, 2),
            CancellationToken.None);
        var producer = new Mock<ITopicProducer<string, BasketCheckedOutIntegrationEvent>>();
        producer
            .Setup(item => item.Produce(customerId.ToString(), It.IsAny<BasketCheckedOutIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Kafka unavailable."));

        var act = async () => await new CheckoutBasketHandler(basketStore, producer.Object).Execute(
            new CheckoutBasketCommand(customerId, "buyer@example.com"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        var items = await basketStore.GetItems(customerId, CancellationToken.None);
        items.Should().ContainSingle();
        items.Single().Quantity.Should().Be(2);
    }

    [Fact]
    public async Task CheckoutBasket_WhenBasketIsEmpty_DoesNotPublishEvent()
    {
        var basketStore = new InMemoryBasketStore();
        var producer = new Mock<ITopicProducer<string, BasketCheckedOutIntegrationEvent>>();

        await new CheckoutBasketHandler(basketStore, producer.Object).Execute(
            new CheckoutBasketCommand(Guid.NewGuid(), "buyer@example.com"),
            CancellationToken.None);

        producer.Verify(
            item => item.Produce(It.IsAny<string>(), It.IsAny<BasketCheckedOutIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
