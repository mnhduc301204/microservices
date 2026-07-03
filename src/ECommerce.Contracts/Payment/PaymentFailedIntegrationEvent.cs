namespace ECommerce.Contracts.Payment;

public sealed record PaymentFailedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid PaymentId,
    Guid OrderId,
    Guid CustomerId,
    string Reason);
