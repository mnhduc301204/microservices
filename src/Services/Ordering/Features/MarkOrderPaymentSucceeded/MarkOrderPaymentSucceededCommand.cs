namespace ECommerce.Ordering.Features.MarkOrderPaymentSucceeded;

public sealed record MarkOrderPaymentSucceededCommand(Guid EventId, Guid OrderId, Guid PaymentId);
