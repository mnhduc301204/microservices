namespace ECommerce.Basket.IntegrationEvents;

public sealed record BasketCheckedOutIntegrationEvent(Guid EventId, DateTimeOffset OccurredAt, Guid CustomerId);
