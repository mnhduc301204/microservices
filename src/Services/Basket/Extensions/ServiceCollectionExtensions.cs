using ECommerce.Basket.Data;
using StackExchange.Redis;

namespace ECommerce.Basket.Extensions;

public static class ServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddBasketData(this IHostApplicationBuilder builder)
    {
        var redisConnectionString = builder.Configuration.GetConnectionString("basket-redis");
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
            builder.Services.AddScoped<IBasketStore, RedisBasketStore>();
        }
        else
        {
            builder.Services.AddSingleton<IBasketStore, InMemoryBasketStore>();
        }

        return builder;
    }
}
