using ECommerce.Payment.Data;
using ECommerce.Payment.Features.CreatePayment;
using ECommerce.Payment.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Payment.Tests;

public sealed class PaymentIdempotencyTests
{
    [Fact]
    public async Task CreatePayment_WithSameIdempotencyKey_CreatesSinglePayment()
    {
        await using var dbContext = CreateDbContext();
        var handler = new CreatePaymentHandler(dbContext);
        var command = new CreatePaymentCommand(Guid.NewGuid(), 25m);

        await handler.Handle(command, "pay-key-1", CancellationToken.None);
        await handler.Handle(command, "pay-key-1", CancellationToken.None);

        (await dbContext.Payments.CountAsync()).Should().Be(1);
        (await dbContext.Set<ECommerce.ServiceDefaults.Messaging.IdempotencyRecord>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public void Payment_WhenSucceededTwice_RemainsSucceededWithProviderTransaction()
    {
        var payment = new ECommerce.Payment.Models.Payment(Guid.NewGuid(), 25m, "usd", "key-1");

        payment.MarkSucceeded("provider-tx-1");
        payment.MarkSucceeded("provider-tx-1");

        payment.Status.Should().Be(PaymentStatus.Succeeded);
        payment.ProviderTransactionId.Should().Be("provider-tx-1");
    }

    private static PaymentDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PaymentDbContext(options);
    }
}
