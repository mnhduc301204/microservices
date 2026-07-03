namespace ECommerce.Payment.IntegrationEvents;

public sealed record PaymentSucceededIntegrationEvent(Guid EventId, DateTimeOffset OccurredAt, Guid PaymentId, Guid OrderId, decimal Amount);
