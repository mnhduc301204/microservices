using System.Collections.Concurrent;
using ECommerce.Basket.Contracts;
using ECommerce.Basket.Models;

namespace ECommerce.Basket.Data;

public sealed class InMemoryBasketStore : IBasketStore
{
    private readonly ConcurrentDictionary<Guid, List<BasketItemDto>> baskets = new();

    public Task<IReadOnlyCollection<BasketItemDto>> GetItems(Guid customerId, CancellationToken cancellationToken)
    {
        var items = baskets.TryGetValue(customerId, out var basketItems)
            ? basketItems.Select(item => item with { }).ToArray()
            : [];

        return Task.FromResult<IReadOnlyCollection<BasketItemDto>>(items);
    }

    public Task<BasketItemDto> AddOrUpdateItem(Guid customerId, string sku, string productName, decimal unitPrice, int quantity, CancellationToken cancellationToken)
    {
        var normalizedSku = BasketItem.NormalizeSku(sku);
        BasketItemDto updated = null!;

        baskets.AddOrUpdate(
            customerId,
            _ =>
            {
                updated = new BasketItemDto(normalizedSku, productName.Trim(), unitPrice, quantity);
                return [updated];
            },
            (_, existingItems) =>
            {
                lock (existingItems)
                {
                    var index = existingItems.FindIndex(item => item.Sku == normalizedSku);
                    if (index < 0)
                    {
                        updated = new BasketItemDto(normalizedSku, productName.Trim(), unitPrice, quantity);
                        existingItems.Add(updated);
                    }
                    else
                    {
                        var existing = existingItems[index];
                        updated = existing with
                        {
                            ProductName = productName.Trim(),
                            UnitPrice = unitPrice,
                            Quantity = existing.Quantity + quantity,
                        };
                        existingItems[index] = updated;
                    }

                    return existingItems;
                }
            });

        return Task.FromResult(updated);
    }

    public Task RemoveItem(Guid customerId, string sku, CancellationToken cancellationToken)
    {
        var normalizedSku = BasketItem.NormalizeSku(sku);
        if (baskets.TryGetValue(customerId, out var items))
        {
            lock (items)
            {
                items.RemoveAll(item => item.Sku == normalizedSku);
                if (items.Count == 0)
                {
                    baskets.TryRemove(customerId, out _);
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task Clear(Guid customerId, CancellationToken cancellationToken)
    {
        baskets.TryRemove(customerId, out _);
        return Task.CompletedTask;
    }
}
