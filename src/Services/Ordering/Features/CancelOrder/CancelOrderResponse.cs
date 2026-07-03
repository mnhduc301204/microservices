using ECommerce.Ordering.Models;

namespace ECommerce.Ordering.Features.CancelOrder;

public sealed record CancelOrderResponse(Guid OrderId, OrderStatus Status);
