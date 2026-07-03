using ECommerce.Ordering.Models;

namespace ECommerce.Ordering.Features.MarkOrderPaymentSucceeded;

public sealed record MarkOrderPaymentSucceededResponse(Guid OrderId, OrderStatus Status);
