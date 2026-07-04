using System.Diagnostics;
using ECommerce.Payment.Data;
using ECommerce.Payment.Features.CreatePayment;
using ECommerce.ServiceDefaults.Messaging;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Payment.Tests;

public sealed class PaymentPerformanceTests
{
    [Fact]
    public async Task CreatePayment_ReplayingSameIdempotencyKey_StaysWithinBudget()
    {
        await using var dbContext = CreateDbContext();
        var handler = new CreatePaymentHandler(dbContext);
        var command = new CreatePaymentCommand(Guid.NewGuid(), 25m);

        await handler.Handle(command, "pay-key-1", CancellationToken.None);
        var started = Stopwatch.StartNew();
        for (var index = 0; index < 1_000; index++)
        {
            await handler.Handle(command, "pay-key-1", CancellationToken.None);
        }

        started.Stop();
        started.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
        (await dbContext.Payments.CountAsync()).Should().Be(1);
        (await dbContext.Set<IdempotencyRecord>().CountAsync()).Should().Be(1);
    }

    private static PaymentDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PaymentDbContext(options);
    }
}
