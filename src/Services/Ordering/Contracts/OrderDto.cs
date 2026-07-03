using ECommerce.Ordering.Models;

namespace ECommerce.Ordering.Contracts;

public sealed record OrderDto(
    Guid Id,
    Guid CustomerId,
    string CustomerEmail,
    OrderStatus Status,
    decimal Total,
    IReadOnlyCollection<OrderItemDto> Items);

public sealed record OrderItemDto(string Sku, string ProductName, decimal UnitPrice, int Quantity);
