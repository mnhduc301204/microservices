namespace ECommerce.Ordering.Models;

public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    private Order()
    {
    }

    public Order(Guid customerId, string customerEmail, IEnumerable<OrderItemDraft> items)
    {
        CustomerId = customerId == Guid.Empty ? throw new ArgumentException("Customer id is required.", nameof(customerId)) : customerId;
        CustomerEmail = string.IsNullOrWhiteSpace(customerEmail) ? throw new ArgumentException("Customer email is required.", nameof(customerEmail)) : customerEmail.Trim();
        Status = OrderStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;

        foreach (var item in items)
        {
            _items.Add(new OrderItem(Id, item.Sku, item.ProductName, item.UnitPrice, item.Quantity));
        }

        if (_items.Count == 0)
        {
            throw new InvalidOperationException("Order requires at least one item.");
        }

        Total = _items.Sum(item => item.UnitPrice * item.Quantity);
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid CustomerId { get; private set; }

    public string CustomerEmail { get; private set; } = string.Empty;

    public OrderStatus Status { get; private set; }

    public decimal Total { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ConfirmedAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items;

    public void ConfirmPayment()
    {
        if (Status == OrderStatus.Confirmed)
        {
            return;
        }

        if (Status is OrderStatus.Cancelled or OrderStatus.Failed)
        {
            throw new InvalidOperationException("Cannot confirm a closed order.");
        }

        Status = OrderStatus.Confirmed;
        ConfirmedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Cancelled)
        {
            return;
        }

        if (Status == OrderStatus.Confirmed)
        {
            throw new InvalidOperationException("Cannot cancel a confirmed order.");
        }

        Status = OrderStatus.Cancelled;
        CancelledAt = DateTimeOffset.UtcNow;
    }

    public void Fail()
    {
        if (Status == OrderStatus.Confirmed)
        {
            throw new InvalidOperationException("Cannot fail a confirmed order.");
        }

        Status = OrderStatus.Failed;
        CancelledAt = DateTimeOffset.UtcNow;
    }
}

public enum OrderStatus
{
    Pending = 1,
    Confirmed = 2,
    Cancelled = 3,
    Failed = 4,
}

public sealed record OrderItemDraft(string Sku, string ProductName, decimal UnitPrice, int Quantity);
