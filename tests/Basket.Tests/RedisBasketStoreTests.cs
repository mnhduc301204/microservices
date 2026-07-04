using ECommerce.Basket.Data;
using FluentAssertions;
using Moq;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace ECommerce.Basket.Tests;

public sealed class RedisBasketStoreTests
{
    [Fact]
    public async Task RedisBasketStore_AddOrUpdateItem_SetsBasketTtl()
    {
        await using var redis = await TryStartRedis();
        if (redis is null)
        {
            return;
        }

        using var connection = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());
        var store = new RedisBasketStore(connection);
        var customerId = Guid.NewGuid();

        await store.AddOrUpdateItem(customerId, "sku-1", "Bottle", 10m, 1, CancellationToken.None);

        var ttl = await connection.GetDatabase().KeyTimeToLiveAsync($"basket:{customerId}");
        ttl.Should().NotBeNull();
        ttl.Should().BeGreaterThan(TimeSpan.FromDays(6));
        ttl.Should().BeLessThanOrEqualTo(TimeSpan.FromDays(7));
    }

    [Fact]
    public async Task RedisBasketStore_Clear_RemovesBasketKey()
    {
        await using var redis = await TryStartRedis();
        if (redis is null)
        {
            return;
        }

        using var connection = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());
        var store = new RedisBasketStore(connection);
        var customerId = Guid.NewGuid();
        await store.AddOrUpdateItem(customerId, "sku-1", "Bottle", 10m, 1, CancellationToken.None);

        await store.Clear(customerId, CancellationToken.None);

        (await connection.GetDatabase().KeyExistsAsync($"basket:{customerId}")).Should().BeFalse();
        (await store.GetItems(customerId, CancellationToken.None)).Should().BeEmpty();
    }

    [Fact]
    public async Task RedisBasketStore_DifferentCustomers_DoNotShareBasketKeys()
    {
        await using var redis = await TryStartRedis();
        if (redis is null)
        {
            return;
        }

        using var connection = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());
        var store = new RedisBasketStore(connection);
        var customerA = Guid.NewGuid();
        var customerB = Guid.NewGuid();

        await store.AddOrUpdateItem(customerA, "sku-1", "Bottle", 10m, 1, CancellationToken.None);
        await store.AddOrUpdateItem(customerB, "sku-2", "Bag", 20m, 2, CancellationToken.None);

        var basketA = await store.GetItems(customerA, CancellationToken.None);
        var basketB = await store.GetItems(customerB, CancellationToken.None);
        basketA.Should().ContainSingle(item => item.Sku == "SKU-1");
        basketB.Should().ContainSingle(item => item.Sku == "SKU-2");
    }

    [Fact]
    public async Task RedisBasketStore_WhenStoredJsonIsInvalid_ThrowsJsonException()
    {
        await using var redis = await TryStartRedis();
        if (redis is null)
        {
            return;
        }

        using var connection = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());
        var customerId = Guid.NewGuid();
        await connection.GetDatabase().StringSetAsync($"basket:{customerId}", "{ invalid json");
        var store = new RedisBasketStore(connection);

        var act = async () => await store.GetItems(customerId, CancellationToken.None);

        await act.Should().ThrowAsync<System.Text.Json.JsonException>();
    }

    [Fact]
    public async Task RedisBasketStore_WhenRedisDatabaseCannotBeResolved_PropagatesFailure()
    {
        var multiplexer = new Mock<IConnectionMultiplexer>();
        multiplexer
            .Setup(item => item.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis unavailable."));

        var act = async () =>
        {
            var store = new RedisBasketStore(multiplexer.Object);
            await store.GetItems(Guid.NewGuid(), CancellationToken.None);
        };

        await act.Should().ThrowAsync<RedisConnectionException>();
    }

    private static async Task<RedisContainer?> TryStartRedis()
    {
        try
        {
            var redis = new RedisBuilder()
                .WithImage("redis:7-alpine")
                .Build();

            await redis.StartAsync();
            return redis;
        }
        catch (Exception ex) when (ex.Message.Contains("Docker", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
    }
}
