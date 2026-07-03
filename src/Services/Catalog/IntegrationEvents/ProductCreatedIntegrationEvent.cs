namespace ECommerce.Catalog.IntegrationEvents;

public sealed record ProductCreatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid ProductId,
    string Sku,
    string Name,
    decimal ListPrice);
