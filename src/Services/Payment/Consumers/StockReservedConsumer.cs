using ECommerce.Contracts;
using ECommerce.Contracts.Inventory;
using ECommerce.Payment.Data;
using ECommerce.ServiceDefaults.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Payment.Consumers;

public sealed class StockReservedConsumer(PaymentDbContext dbContext) : IConsumer<StockReservedIntegrationEvent>
{
    private const string ConsumerName = nameof(StockReservedConsumer);

    public async Task Consume(ConsumeContext<StockReservedIntegrationEvent> context)
    {
        var message = context.Message;
        if (!await dbContext.TryBeginProcessingAsync(message.EventId, ConsumerName, context.CancellationToken))
        {
            return;
        }

        var payment = await dbContext.Payments.FirstOrDefaultAsync(payment => payment.OrderId == message.OrderId, context.CancellationToken);
        if (payment is null)
        {
            payment = new Models.Payment(message.OrderId, message.Total, "USD");
            dbContext.Payments.Add(payment);
        }

        if (payment.Status == Models.PaymentStatus.Pending && string.IsNullOrWhiteSpace(payment.ProviderIntentId))
        {
            payment.RequestProviderIntent($"fake-intent-{payment.Id:N}");
        }

        dbContext.MarkProcessed(message.EventId, ConsumerName);
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
