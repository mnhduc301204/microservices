namespace ECommerce.Contracts.Basket;

public sealed record BasketCheckedOutIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid CustomerId,
    string CustomerEmail,
    IReadOnlyCollection<BasketCheckedOutLine> Lines);

public sealed record BasketCheckedOutLine(string Sku, string ProductName, decimal UnitPrice, int Quantity);
