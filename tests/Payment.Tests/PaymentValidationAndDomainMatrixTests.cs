using ECommerce.Payment.Features.ConfirmPayment;
using ECommerce.Payment.Features.CreatePayment;
using ECommerce.Payment.Features.RefundPayment;
using ECommerce.Payment.Models;
using FluentAssertions;
using PaymentEntity = ECommerce.Payment.Models.Payment;

namespace ECommerce.Payment.Tests;

public sealed class PaymentValidationAndDomainMatrixTests
{
    [Theory]
    [MemberData(nameof(CreatePaymentCommands))]
    public void CreatePaymentValidator_ValidatesInput(CreatePaymentCommand command, bool expected)
    {
        new CreatePaymentValidator().Validate(command).IsValid.Should().Be(expected);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConfirmPaymentValidator_ValidatesPaymentId(bool validPaymentId)
    {
        var command = new ConfirmPaymentCommand(validPaymentId ? Guid.NewGuid() : Guid.Empty);

        new ConfirmPaymentValidator().Validate(command).IsValid.Should().Be(validPaymentId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RefundPaymentValidator_ValidatesPaymentId(bool validPaymentId)
    {
        var command = new RefundPaymentCommand(validPaymentId ? Guid.NewGuid() : Guid.Empty);

        new RefundPaymentValidator().Validate(command).IsValid.Should().Be(validPaymentId);
    }

    [Theory]
    [InlineData(1, "USD", "USD")]
    [InlineData(10.5, "usd", "USD")]
    [InlineData(99.99, " eur ", "EUR")]
    public void Payment_Constructor_NormalizesCurrency(decimal amount, string currency, string expectedCurrency)
    {
        var payment = new PaymentEntity(Guid.NewGuid(), amount, currency);

        payment.Amount.Should().Be(amount);
        payment.Currency.Should().Be(expectedCurrency);
        payment.Status.Should().Be(PaymentStatus.Pending);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-1)]
    public void Payment_Constructor_WhenAmountIsNotPositive_Throws(decimal amount)
    {
        Action act = () => _ = new PaymentEntity(Guid.NewGuid(), amount, "USD");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Payment_RequestProviderIntent_WhenIntentIsBlank_Throws(string providerIntentId)
    {
        var payment = new PaymentEntity(Guid.NewGuid(), 10m, "USD");

        Action act = () => payment.RequestProviderIntent(providerIntentId);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("tx-1")]
    [InlineData("")]
    [InlineData(null)]
    public void Payment_MarkSucceeded_IsIdempotent(string? providerTransactionId)
    {
        var payment = new PaymentEntity(Guid.NewGuid(), 10m, "USD");

        payment.MarkSucceeded(providerTransactionId);
        payment.MarkSucceeded(providerTransactionId);

        payment.Status.Should().Be(PaymentStatus.Succeeded);
    }

    [Theory]
    [InlineData("declined", "declined")]
    [InlineData("", "Payment failed.")]
    [InlineData(null, "Payment failed.")]
    public void Payment_MarkFailed_StoresReason(string? reason, string expectedReason)
    {
        var payment = new PaymentEntity(Guid.NewGuid(), 10m, "USD");

        payment.MarkFailed(reason);

        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.FailureReason.Should().Be(expectedReason);
    }

    [Fact]
    public void Payment_MarkFailed_AfterSucceeded_ThrowsAndKeepsSucceeded()
    {
        var payment = new PaymentEntity(Guid.NewGuid(), 10m, "USD");
        payment.MarkSucceeded("tx-1");

        Action act = () => payment.MarkFailed("late failure");

        act.Should().Throw<InvalidOperationException>();
        payment.Status.Should().Be(PaymentStatus.Succeeded);
    }

    [Theory]
    [InlineData(PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Failed)]
    public void Payment_Refund_WhenNotSucceeded_Throws(PaymentStatus status)
    {
        var payment = new PaymentEntity(Guid.NewGuid(), 10m, "USD");
        if (status == PaymentStatus.Failed)
        {
            payment.MarkFailed();
        }

        Action act = () => payment.Refund();

        act.Should().Throw<InvalidOperationException>();
    }

    public static TheoryData<CreatePaymentCommand, bool> CreatePaymentCommands()
    {
        var valid = new CreatePaymentCommand(Guid.NewGuid(), 10m, "USD");
        var data = new TheoryData<CreatePaymentCommand, bool>();
        data.Add(valid, true);
        data.Add(valid with { OrderId = Guid.Empty }, false);
        data.Add(valid with { Amount = 0m }, false);
        data.Add(valid with { Amount = -0.01m }, false);
        data.Add(valid with { Amount = -1m }, false);
        data.Add(valid with { Currency = "" }, false);
        data.Add(valid with { Currency = " " }, false);
        data.Add(valid with { Currency = "US" }, false);
        data.Add(valid with { Currency = "USDD" }, false);
        data.Add(valid with { Currency = "usd" }, true);
        data.Add(valid with { Currency = "EUR" }, true);
        return data;
    }
}
