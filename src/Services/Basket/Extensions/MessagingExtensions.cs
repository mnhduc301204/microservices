using ECommerce.Contracts;
using ECommerce.Contracts.Basket;
using ECommerce.Basket.Features.AddItemToBasket;
using ECommerce.Basket.Features.CheckoutBasket;
using ECommerce.ServiceDefaults.Messaging;
using MassTransit;

namespace ECommerce.Basket.Extensions;

public static class MessagingExtensions
{
    public static IHostApplicationBuilder AddBasketMessaging(this IHostApplicationBuilder builder)
    {
        builder.Services.AddMediator(cfg =>
        {
            cfg.AddConsumers(typeof(Program).Assembly);
            cfg.AddRequestClient<AddItemToBasketCommand>();
            cfg.AddRequestClient<CheckoutBasketCommand>();
        });
        builder.Services.AddMassTransit(bus =>
        {
            bus.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
            bus.AddRider(rider =>
            {
                rider.AddProducer<BasketCheckedOutIntegrationEvent>(KafkaTopics.BasketCheckedOut);
                rider.UsingKafka((_, kafka) => kafka.Host(KafkaConnection.GetBootstrapServers(builder.Configuration)));
            });
        });

        return builder;
    }
}
