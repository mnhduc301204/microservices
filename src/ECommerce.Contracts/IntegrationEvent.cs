namespace ECommerce.Contracts;

public abstract record IntegrationEvent(Guid EventId, DateTimeOffset OccurredAt);
