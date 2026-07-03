using ECommerce.Ordering.Data;
using ECommerce.Ordering.Models;
using ECommerce.Contracts;
using ECommerce.Contracts.Ordering;
using ECommerce.ServiceDefaults;
using ECommerce.ServiceDefaults.Messaging;
using FluentValidation;

namespace ECommerce.Ordering.Features.CreateOrder;

public sealed class CreateOrderHandler(OrderingDbContext dbContext)
{
    public async Task<IResult> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        return (await Execute(command, cancellationToken)).ToHttpResult();
    }

    public async Task<OperationResult<CreateOrderResponse>> Execute(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var validation = await new CreateOrderValidator().ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return OperationResult<CreateOrderResponse>.Validation(validation.ToDictionary());
        }

        var drafts = command.Items.Select(item => new OrderItemDraft(item.Sku, item.ProductName, item.UnitPrice, item.Quantity));
        var order = new Order(command.CustomerId, command.CustomerEmail, drafts);

        dbContext.Orders.Add(order);
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
        await dbContext.SaveChangesAsync(cancellationToken);

        return OperationResult<CreateOrderResponse>.Created(
            $"/api/orders/{order.Id}",
            new CreateOrderResponse(order.Id, order.Status, order.Total));
    }
}
