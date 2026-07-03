namespace ECommerce.Ordering.Contracts;

public sealed record CreateOrderRequest(Guid CustomerId, string CustomerEmail, IReadOnlyCollection<CreateOrderItemRequest> Items);

public sealed record CreateOrderItemRequest(string Sku, string ProductName, decimal UnitPrice, int Quantity);
