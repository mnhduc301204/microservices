using ECommerce.Ordering.Features.CancelOrder;
using ECommerce.Ordering.Features.CreateOrder;
using ECommerce.Ordering.Features.MarkOrderPaymentSucceeded;
using ECommerce.Ordering.Models;
using FluentAssertions;

namespace ECommerce.Ordering.Tests;

public sealed class OrderingValidationAndStateMatrixTests
{
    [Theory]
    [MemberData(nameof(CreateOrderCommands))]
    public void CreateOrderValidator_ValidatesInput(CreateOrderCommand command, bool expected)
    {
        new CreateOrderValidator().Validate(command).IsValid.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(CancelOrderCommands))]
    public void CancelOrderValidator_ValidatesInput(CancelOrderCommand command, bool expected)
    {
        new CancelOrderValidator().Validate(command).IsValid.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(MarkPaymentSucceededCommands))]
    public void MarkOrderPaymentSucceededValidator_ValidatesInput(MarkOrderPaymentSucceededCommand command, bool expected)
    {
        new MarkOrderPaymentSucceededValidator().Validate(command).IsValid.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(OrderTransitions))]
    public void Order_StateTransitions_AreIdempotentOrRejected(Action<Order> arrange, Action<Order> act, OrderStatus expected, bool throws)
    {
        var order = NewOrder();
        arrange(order);

        if (throws)
        {
            Action call = () => act(order);
            call.Should().Throw<InvalidOperationException>();
            return;
        }

        act(order);
        order.Status.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(SagaTransitions))]
    public void CheckoutSagaState_Transitions_AreTerminalWhenCompletedOrFailed(Action<CheckoutSagaState> arrange, Action<CheckoutSagaState> act, CheckoutSagaStatus expected)
    {
        var saga = new CheckoutSagaState(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        arrange(saga);

        act(saga);

        saga.Status.Should().Be(expected);
    }

    public static TheoryData<CreateOrderCommand, bool> CreateOrderCommands()
    {
        var valid = ValidCreateOrder();
        var data = new TheoryData<CreateOrderCommand, bool>();
        data.Add(valid, true);
        data.Add(valid with { CustomerId = Guid.Empty }, false);
        data.Add(valid with { CustomerEmail = "" }, false);
        data.Add(valid with { CustomerEmail = " " }, false);
        data.Add(valid with { CustomerEmail = "not-email" }, false);
        data.Add(valid with { CustomerEmail = $"{new string('a', 314)}@x.com" }, true);
        data.Add(valid with { CustomerEmail = $"{new string('a', 315)}@x.com" }, false);
        data.Add(valid with { Items = [] }, false);
        data.Add(valid with { Items = [ValidLine() with { Sku = "" }] }, false);
        data.Add(valid with { Items = [ValidLine() with { Sku = " " }] }, false);
        data.Add(valid with { Items = [ValidLine() with { Sku = new string('S', 64) }] }, true);
        data.Add(valid with { Items = [ValidLine() with { Sku = new string('S', 65) }] }, false);
        data.Add(valid with { Items = [ValidLine() with { ProductName = "" }] }, false);
        data.Add(valid with { Items = [ValidLine() with { ProductName = " " }] }, false);
        data.Add(valid with { Items = [ValidLine() with { ProductName = new string('P', 200) }] }, true);
        data.Add(valid with { Items = [ValidLine() with { ProductName = new string('P', 201) }] }, false);
        data.Add(valid with { Items = [ValidLine() with { UnitPrice = 0m }] }, true);
        data.Add(valid with { Items = [ValidLine() with { UnitPrice = -0.01m }] }, false);
        data.Add(valid with { Items = [ValidLine() with { Quantity = 1 }] }, true);
        data.Add(valid with { Items = [ValidLine() with { Quantity = 0 }] }, false);
        data.Add(valid with { Items = [ValidLine() with { Quantity = -1 }] }, false);
        data.Add(valid with { Items = [ValidLine(), ValidLine() with { Sku = "SKU-2" }] }, true);
        return data;
    }

    public static TheoryData<CancelOrderCommand, bool> CancelOrderCommands()
    {
        var data = new TheoryData<CancelOrderCommand, bool>();
        data.Add(new CancelOrderCommand(Guid.NewGuid()), true);
        data.Add(new CancelOrderCommand(Guid.Empty), false);
        return data;
    }

    public static TheoryData<MarkOrderPaymentSucceededCommand, bool> MarkPaymentSucceededCommands()
    {
        var valid = new MarkOrderPaymentSucceededCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var data = new TheoryData<MarkOrderPaymentSucceededCommand, bool>();
        data.Add(valid, true);
        data.Add(valid with { EventId = Guid.Empty }, false);
        data.Add(valid with { OrderId = Guid.Empty }, false);
        data.Add(valid with { PaymentId = Guid.Empty }, false);
        return data;
    }

    public static TheoryData<Action<Order>, Action<Order>, OrderStatus, bool> OrderTransitions()
    {
        var data = new TheoryData<Action<Order>, Action<Order>, OrderStatus, bool>();
        data.Add(_ => { }, order => order.ConfirmPayment(), OrderStatus.Confirmed, false);
        data.Add(order => order.ConfirmPayment(), order => order.ConfirmPayment(), OrderStatus.Confirmed, false);
        data.Add(_ => { }, order => order.Cancel(), OrderStatus.Cancelled, false);
        data.Add(order => order.Cancel(), order => order.Cancel(), OrderStatus.Cancelled, false);
        data.Add(_ => { }, order => order.Fail(), OrderStatus.Failed, false);
        data.Add(order => order.ConfirmPayment(), order => order.Cancel(), OrderStatus.Confirmed, true);
        data.Add(order => order.ConfirmPayment(), order => order.Fail(), OrderStatus.Confirmed, true);
        data.Add(order => order.Cancel(), order => order.ConfirmPayment(), OrderStatus.Cancelled, true);
        data.Add(order => order.Fail(), order => order.ConfirmPayment(), OrderStatus.Failed, true);
        return data;
    }

    public static TheoryData<Action<CheckoutSagaState>, Action<CheckoutSagaState>, CheckoutSagaStatus> SagaTransitions()
    {
        var data = new TheoryData<Action<CheckoutSagaState>, Action<CheckoutSagaState>, CheckoutSagaStatus>();
        data.Add(_ => { }, saga => saga.MarkStockReserved(Guid.NewGuid()), CheckoutSagaStatus.StockReserved);
        data.Add(saga => saga.MarkStockReserved(Guid.NewGuid()), saga => saga.MarkPaymentSucceeded(Guid.NewGuid()), CheckoutSagaStatus.Completed);
        data.Add(saga => saga.MarkStockReserved(Guid.NewGuid()), saga => saga.MarkFailed("payment failed"), CheckoutSagaStatus.Failed);
        data.Add(saga => saga.MarkPaymentSucceeded(Guid.NewGuid()), saga => saga.MarkFailed("late failure"), CheckoutSagaStatus.Completed);
        data.Add(saga => saga.MarkPaymentSucceeded(Guid.NewGuid()), saga => saga.MarkStockReserved(Guid.NewGuid()), CheckoutSagaStatus.Completed);
        data.Add(saga => saga.MarkFailed("reserve failed"), saga => saga.MarkStockReserved(Guid.NewGuid()), CheckoutSagaStatus.Failed);
        data.Add(saga => saga.MarkFailed("reserve failed"), saga => saga.MarkPaymentSucceeded(Guid.NewGuid()), CheckoutSagaStatus.Failed);
        return data;
    }

    private static CreateOrderCommand ValidCreateOrder() =>
        new(Guid.NewGuid(), "buyer@example.com", [ValidLine()]);

    private static CreateOrderLine ValidLine() =>
        new("SKU-1", "Bottle", 10m, 1);

    private static Order NewOrder() =>
        new(Guid.NewGuid(), "buyer@example.com", [new OrderItemDraft("SKU-1", "Bottle", 10m, 1)]);
}
