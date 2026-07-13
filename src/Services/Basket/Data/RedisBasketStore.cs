using System.Text.Json;
using ECommerce.Basket.Contracts;
using ECommerce.Basket.Models;
using StackExchange.Redis;

namespace ECommerce.Basket.Data;

public sealed class RedisBasketStore(IConnectionMultiplexer connectionMultiplexer) : IBasketStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan BasketTtl = TimeSpan.FromDays(7);

    private readonly IDatabase database = connectionMultiplexer.GetDatabase();

    public async Task<IReadOnlyCollection<BasketItemDto>> GetItems(Guid customerId, CancellationToken cancellationToken)
    {
        var value = await database.StringGetAsync(GetKey(customerId));
        return Deserialize(value);
    }

    public async Task<BasketCheckoutState?> GetPendingCheckout(Guid customerId, CancellationToken cancellationToken)
    {
        var value = await database.StringGetAsync(GetCheckoutKey(customerId));
        if (value.IsNullOrEmpty)
        {
            return null;
        }

        return JsonSerializer.Deserialize<BasketCheckoutState>(value.ToString(), JsonOptions);
    }

    public async Task<BasketCheckoutState> CreatePendingCheckout(
        Guid customerId,
        Guid checkoutId,
        IReadOnlyCollection<BasketItemDto> items,
        CancellationToken cancellationToken)
    {
        var checkoutKey = GetCheckoutKey(customerId);
        BasketCheckoutState checkout = null!;

        await WithLock(customerId, async () =>
        {
            var existing = await database.StringGetAsync(checkoutKey);
            if (!existing.IsNullOrEmpty)
            {
                checkout = JsonSerializer.Deserialize<BasketCheckoutState>(existing.ToString(), JsonOptions)!;
                return;
            }

            checkout = new BasketCheckoutState(checkoutId, items.Select(item => item with { }).ToArray());
            await database.StringSetAsync(checkoutKey, JsonSerializer.Serialize(checkout, JsonOptions), BasketTtl);
        });

        return checkout;
    }

    public async Task<BasketItemDto> AddOrUpdateItem(Guid customerId, string sku, string productName, decimal unitPrice, int quantity, CancellationToken cancellationToken)
    {
        var normalizedSku = BasketItem.NormalizeSku(sku);
        var key = GetKey(customerId);
        BasketItemDto updated = null!;

        await WithLock(customerId, async () =>
        {
            var items = Deserialize(await database.StringGetAsync(key)).ToList();
            var index = items.FindIndex(item => item.Sku == normalizedSku);
            if (index < 0)
            {
                updated = new BasketItemDto(normalizedSku, productName.Trim(), unitPrice, quantity);
                items.Add(updated);
            }
            else
            {
                var existing = items[index];
                updated = existing with
                {
                    ProductName = productName.Trim(),
                    UnitPrice = unitPrice,
                    Quantity = existing.Quantity + quantity,
                };
                items[index] = updated;
            }

            await SaveItems(key, items);
        });

        return updated;

        async Task SaveItems(RedisKey basketKey, List<BasketItemDto> items)
        {
            if (items.Count == 0)
            {
                await database.KeyDeleteAsync(basketKey);
                return;
            }

            await database.StringSetAsync(basketKey, JsonSerializer.Serialize(items, JsonOptions), BasketTtl);
        }
    }

    public async Task RemoveItem(Guid customerId, string sku, CancellationToken cancellationToken)
    {
        var normalizedSku = BasketItem.NormalizeSku(sku);
        var key = GetKey(customerId);

        await WithLock(customerId, async () =>
        {
            var items = Deserialize(await database.StringGetAsync(key)).ToList();
            items.RemoveAll(item => item.Sku == normalizedSku);

            if (items.Count == 0)
            {
                await database.KeyDeleteAsync(key);
            }
            else
            {
                await database.StringSetAsync(key, JsonSerializer.Serialize(items, JsonOptions), BasketTtl);
            }
        });
    }

    public Task Clear(Guid customerId, CancellationToken cancellationToken) =>
        database.KeyDeleteAsync(GetKey(customerId));

    public async Task CompleteCheckout(Guid customerId, Guid checkoutId, CancellationToken cancellationToken)
    {
        await WithLock(customerId, async () =>
        {
            var checkoutKey = GetCheckoutKey(customerId);
            var checkoutValue = await database.StringGetAsync(checkoutKey);
            if (checkoutValue.IsNullOrEmpty)
            {
                return;
            }

            var checkout = JsonSerializer.Deserialize<BasketCheckoutState>(checkoutValue.ToString(), JsonOptions);
            if (checkout is null || checkout.CheckoutId != checkoutId)
            {
                return;
            }

            var basketKey = GetKey(customerId);
            var currentItems = Deserialize(await database.StringGetAsync(basketKey)).ToList();
            foreach (var checkedOutItem in checkout.Items)
            {
                var index = currentItems.FindIndex(item => item.Sku == checkedOutItem.Sku);
                if (index < 0)
                {
                    continue;
                }

                var remainingQuantity = currentItems[index].Quantity - checkedOutItem.Quantity;
                if (remainingQuantity <= 0)
                {
                    currentItems.RemoveAt(index);
                }
                else
                {
                    currentItems[index] = currentItems[index] with { Quantity = remainingQuantity };
                }
            }

            if (currentItems.Count == 0)
            {
                await database.KeyDeleteAsync(basketKey);
            }
            else
            {
                await database.StringSetAsync(basketKey, JsonSerializer.Serialize(currentItems, JsonOptions), BasketTtl);
            }

            await database.KeyDeleteAsync(checkoutKey);
        });
    }

    private async Task WithLock(Guid customerId, Func<Task> action)
    {
        var lockKey = $"basket:{customerId}:lock";
        var lockValue = Guid.NewGuid().ToString("N");

        if (!await database.LockTakeAsync(lockKey, lockValue, TimeSpan.FromSeconds(10)))
        {
            throw new InvalidOperationException("Could not acquire basket lock.");
        }

        try
        {
            await action();
        }
        finally
        {
            await database.LockReleaseAsync(lockKey, lockValue);
        }
    }

    private static RedisKey GetKey(Guid customerId) => $"basket:{customerId}";

    private static RedisKey GetCheckoutKey(Guid customerId) => $"basket:{customerId}:checkout";

    private static IReadOnlyCollection<BasketItemDto> Deserialize(RedisValue value)
    {
        if (value.IsNullOrEmpty)
        {
            return [];
        }

        return JsonSerializer.Deserialize<IReadOnlyCollection<BasketItemDto>>(value.ToString(), JsonOptions) ?? [];
    }
}
