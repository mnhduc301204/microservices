namespace ECommerce.Basket.Models;

public sealed class CustomerBasket
{
    private readonly List<BasketItem> _items = [];

    private CustomerBasket()
    {
    }

    public CustomerBasket(Guid customerId)
    {
        CustomerId = customerId == Guid.Empty ? throw new ArgumentException("Customer id is required.", nameof(customerId)) : customerId;
    }

    public Guid CustomerId { get; private set; }

    public IReadOnlyCollection<BasketItem> Items => _items;

    public void AddOrUpdateItem(string sku, string productName, decimal unitPrice, int quantity)
    {
        var existing = _items.FirstOrDefault(item => item.Sku == BasketItem.NormalizeSku(sku));
        if (existing is null)
        {
            _items.Add(new BasketItem(CustomerId, sku, productName, unitPrice, quantity));
            return;
        }

        existing.ChangeQuantity(existing.Quantity + quantity);
    }

    public void RemoveItem(string sku)
    {
        var item = _items.FirstOrDefault(item => item.Sku == BasketItem.NormalizeSku(sku));
        if (item is not null)
        {
            _items.Remove(item);
        }
    }

    public void Clear() => _items.Clear();
}
