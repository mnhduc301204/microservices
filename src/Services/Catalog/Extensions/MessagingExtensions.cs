using ECommerce.Catalog.Data;
using ECommerce.Catalog.Features.CreateProduct;
using ECommerce.Contracts;
using ECommerce.Contracts.Catalog;
using ECommerce.ServiceDefaults.Messaging;
using MassTransit;

namespace ECommerce.Catalog.Extensions;

public static class MessagingExtensions
{
    public static IHostApplicationBuilder AddCatalogMessaging(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<KafkaOutboxPublisher>();
        builder.Services.AddHostedService<OutboxDispatcher<CatalogDbContext>>();
        builder.Services.AddMediator(cfg =>
        {
            cfg.AddConsumers(typeof(Program).Assembly);
            cfg.AddRequestClient<CreateProductCommand>();
        });
        builder.Services.AddMassTransit(bus =>
        {
            bus.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
            bus.AddRider(rider =>
            {
                rider.AddProducer<ProductCreatedIntegrationEvent>(KafkaTopics.ProductCreated);
                rider.UsingKafka((_, kafka) => kafka.Host(KafkaConnection.GetBootstrapServers(builder.Configuration)));
            });
        });

        return builder;
    }
}
