namespace ECommerce.Contracts.Payment;

public sealed record PaymentSucceededIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid PaymentId,
    Guid OrderId,
    Guid CustomerId,
    string CustomerEmail,
    decimal Amount);
