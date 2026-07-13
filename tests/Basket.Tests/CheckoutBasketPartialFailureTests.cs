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
    public async Task CheckoutBasket_WhenPublishIsRetried_ReusesPendingCheckoutId()
    {
        var basketStore = new InMemoryBasketStore();
        var customerId = Guid.NewGuid();
        await new AddItemToBasketHandler(basketStore).Execute(
            new AddItemToBasketCommand(customerId, "sku-1", "Bottle", 12m, 2),
            CancellationToken.None);
        var published = new List<BasketCheckedOutIntegrationEvent>();
        var producer = new Mock<ITopicProducer<string, BasketCheckedOutIntegrationEvent>>();
        producer
            .Setup(item => item.Produce(customerId.ToString(), It.IsAny<BasketCheckedOutIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<string, BasketCheckedOutIntegrationEvent, CancellationToken>((_, message, _) => published.Add(message))
            .ThrowsAsync(new InvalidOperationException("Kafka unavailable."));
        var handler = new CheckoutBasketHandler(basketStore, producer.Object);
        var command = new CheckoutBasketCommand(customerId, "buyer@example.com");

        await FluentActions.Invoking(async () => await handler.Execute(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();
        await FluentActions.Invoking(async () => await handler.Execute(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();

        published.Should().HaveCount(2);
        published.Select(message => message.EventId).Distinct().Should().ContainSingle();
    }

    [Fact]
    public async Task CheckoutBasket_WhenItemsChangeBeforeRetry_PreservesNewQuantitiesAndItems()
    {
        var basketStore = new InMemoryBasketStore();
        var customerId = Guid.NewGuid();
        var addItem = new AddItemToBasketHandler(basketStore);
        await addItem.Execute(new AddItemToBasketCommand(customerId, "sku-1", "Bottle", 12m, 2), CancellationToken.None);

        var producer = new Mock<ITopicProducer<string, BasketCheckedOutIntegrationEvent>>();
        producer
            .SetupSequence(item => item.Produce(customerId.ToString(), It.IsAny<BasketCheckedOutIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Kafka unavailable."))
            .Returns(Task.CompletedTask);
        var handler = new CheckoutBasketHandler(basketStore, producer.Object);
        var command = new CheckoutBasketCommand(customerId, "buyer@example.com");

        await FluentActions.Invoking(() => handler.Execute(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();
        await addItem.Execute(new AddItemToBasketCommand(customerId, "sku-1", "Bottle", 12m, 3), CancellationToken.None);
        await addItem.Execute(new AddItemToBasketCommand(customerId, "sku-2", "Glass", 8m, 1), CancellationToken.None);

        await handler.Execute(command, CancellationToken.None);

        var remaining = await basketStore.GetItems(customerId, CancellationToken.None);
        remaining.Should().BeEquivalentTo(
            new BasketItemDto("SKU-1", "Bottle", 12m, 3),
            new BasketItemDto("SKU-2", "Glass", 8m, 1));
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
