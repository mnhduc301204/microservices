namespace ECommerce.Contracts.Payment;

public sealed record PaymentRefundRequestedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid PaymentId,
    Guid OrderId,
    Guid CustomerId,
    string Reason);
