using ECommerce.Ordering.Models;

namespace ECommerce.Ordering.Features.CreateOrder;

public sealed record CreateOrderResponse(Guid OrderId, OrderStatus Status, decimal Total);
