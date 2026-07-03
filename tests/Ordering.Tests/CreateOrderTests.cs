using ECommerce.Ordering.Data;
using ECommerce.Ordering.Features.CreateOrder;
using ECommerce.Ordering.Features.MarkOrderPaymentSucceeded;
using ECommerce.Ordering.Models;
using ECommerce.Contracts;
using ECommerce.ServiceDefaults.Messaging;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Tests;

public sealed class CreateOrderTests
{
    [Fact]
    public async Task CreateOrder_WithItems_CreatesPendingOrder()
    {
        await using var dbContext = CreateDbContext();

        await new CreateOrderHandler(dbContext).Handle(
            new CreateOrderCommand(
                Guid.NewGuid(),
                "buyer@example.com",
                [new CreateOrderLine("sku-1", "Bottle", 12m, 2)]),
            CancellationToken.None);

        var order = await dbContext.Orders.Include(order => order.Items).SingleAsync();
        order.Status.Should().Be(OrderStatus.Pending);
        order.Total.Should().Be(24m);
        order.Items.Should().ContainSingle();

        var outboxMessage = await dbContext.Set<OutboxMessage>().SingleAsync();
        outboxMessage.Topic.Should().Be(KafkaTopics.OrderCreated);
        outboxMessage.MessageType.Should().Contain("OrderCreatedIntegrationEvent");
    }

    [Fact]
    public async Task PaymentSucceededHandler_WhenDuplicateEvent_ConfirmsOrderOnlyOnce()
    {
        await using var dbContext = CreateDbContext();
        var order = new Order(
            Guid.NewGuid(),
            "buyer@example.com",
            [new OrderItemDraft("sku-1", "Bottle", 12m, 1)]);
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var eventId = Guid.NewGuid();
        var handler = new MarkOrderPaymentSucceededHandler(dbContext);

        await handler.Handle(new MarkOrderPaymentSucceededCommand(eventId, order.Id, Guid.NewGuid()), CancellationToken.None);
        await handler.Handle(new MarkOrderPaymentSucceededCommand(eventId, order.Id, Guid.NewGuid()), CancellationToken.None);

        var updated = await dbContext.Orders.SingleAsync();
        updated.Status.Should().Be(OrderStatus.Confirmed);
        (await dbContext.ProcessedMessages.CountAsync()).Should().Be(1);
    }

    [Fact]
    public void CheckoutSaga_WhenPaymentFailsAfterStockReservation_TracksFailureAndReservation()
    {
        var orderId = Guid.NewGuid();
        var saga = new CheckoutSagaState(Guid.NewGuid(), orderId, Guid.NewGuid());
        var reservationId = Guid.NewGuid();

        saga.MarkStockReserved(reservationId);
        saga.MarkFailed("Payment failed.");

        saga.OrderId.Should().Be(orderId);
        saga.StockReservationId.Should().Be(reservationId);
        saga.Status.Should().Be(CheckoutSagaStatus.Failed);
        saga.FailureReason.Should().Be("Payment failed.");
        saga.CompletedAt.Should().NotBeNull();
    }

    private static OrderingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OrderingDbContext(options);
    }
}
