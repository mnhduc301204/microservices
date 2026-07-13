namespace ECommerce.Contracts.Payment;

public sealed record PaymentRefundedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid PaymentId,
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string Reason);
