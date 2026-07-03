using ECommerce.Contracts;
using ECommerce.Contracts.Basket;
using ECommerce.Contracts.Ordering;
using ECommerce.Ordering.Data;
using ECommerce.Ordering.Models;
using ECommerce.ServiceDefaults.Messaging;
using MassTransit;

namespace ECommerce.Ordering.Consumers;

public sealed class BasketCheckedOutConsumer(OrderingDbContext dbContext) : IConsumer<BasketCheckedOutIntegrationEvent>
{
    private const string ConsumerName = nameof(BasketCheckedOutConsumer);

    public async Task Consume(ConsumeContext<BasketCheckedOutIntegrationEvent> context)
    {
        var message = context.Message;
        if (!await dbContext.TryBeginProcessingAsync(message.EventId, ConsumerName, context.CancellationToken))
        {
            return;
        }

        var drafts = message.Lines.Select(line => new OrderItemDraft(line.Sku, line.ProductName, line.UnitPrice, line.Quantity));
        var order = new Order(message.CustomerId, message.CustomerEmail, drafts);
        var saga = new CheckoutSagaState(message.EventId, order.Id, order.CustomerId);

        dbContext.Orders.Add(order);
        dbContext.CheckoutSagas.Add(saga);
        dbContext.Set<OutboxMessage>().Add(OutboxMessage.Create(
            KafkaTopics.OrderCreated,
            new OrderCreatedIntegrationEvent(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                order.Id,
                order.CustomerId,
                order.CustomerEmail,
                order.Total,
                order.Items.Select(item => new OrderCreatedLine(item.Sku, item.ProductName, item.UnitPrice, item.Quantity)).ToArray())));
        dbContext.MarkProcessed(message.EventId, ConsumerName);
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
