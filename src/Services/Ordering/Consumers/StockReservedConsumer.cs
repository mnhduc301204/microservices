using ECommerce.Contracts.Inventory;
using ECommerce.Ordering.Data;
using ECommerce.ServiceDefaults.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Consumers;

public sealed class StockReservedConsumer(OrderingDbContext dbContext) : IConsumer<StockReservedIntegrationEvent>
{
    private const string ConsumerName = nameof(StockReservedConsumer);

    public async Task Consume(ConsumeContext<StockReservedIntegrationEvent> context)
    {
        var message = context.Message;
        if (await dbContext.HasProcessedAsync(message.EventId, ConsumerName, context.CancellationToken))
        {
            return;
        }

        var saga = await dbContext.CheckoutSagas.FirstOrDefaultAsync(saga => saga.OrderId == message.OrderId, context.CancellationToken);
        saga?.MarkStockReserved(message.ReservationId);

        dbContext.MarkProcessed(message.EventId, ConsumerName);
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
