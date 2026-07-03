using ECommerce.Ordering.Contracts;
using ECommerce.Ordering.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Features.GetOrder;

public sealed class GetOrderHandler(OrderingDbContext dbContext)
{
    public async Task<IResult> Handle(GetOrderQuery query, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.AsNoTracking()
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == query.OrderId, cancellationToken);
        if (order is null)
        {
            return Results.NotFound();
        }

        var dto = new OrderDto(
            order.Id,
            order.CustomerId,
            order.CustomerEmail,
            order.Status,
            order.Total,
            order.Items.Select(item => new OrderItemDto(item.Sku, item.ProductName, item.UnitPrice, item.Quantity)).ToArray());

        return Results.Ok(new GetOrderResponse(dto));
    }
}
