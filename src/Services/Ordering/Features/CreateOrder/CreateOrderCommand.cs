namespace ECommerce.Ordering.Features.CreateOrder;

public sealed record CreateOrderCommand(Guid CustomerId, string CustomerEmail, IReadOnlyCollection<CreateOrderLine> Items);

public sealed record CreateOrderLine(string Sku, string ProductName, decimal UnitPrice, int Quantity);
