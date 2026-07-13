using ECommerce.Contracts;
using ECommerce.Contracts.Inventory;
using ECommerce.Contracts.Ordering;
using ECommerce.Inventory.Consumers;
using ECommerce.Inventory.Data;
using ECommerce.ServiceDefaults.Messaging;
using MassTransit;

namespace ECommerce.Inventory.Extensions;

public static class MessagingExtensions
{
    public static IHostApplicationBuilder AddInventoryMessaging(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<KafkaOutboxPublisher>();
        builder.Services.AddHostedService<OutboxDispatcher<InventoryDbContext>>();
        builder.Services.AddMediator(cfg => cfg.AddConsumers(typeof(Program).Assembly));
        builder.Services.AddMassTransit(bus =>
        {
            bus.AddConsumer<OrderCreatedConsumer>();
            bus.AddConsumer<OrderConfirmedConsumer>();
            bus.AddConsumer<ReleaseStockReservationConsumer>();
            bus.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
            bus.AddRider(rider =>
            {
                rider.AddProducer<string, StockReservedIntegrationEvent>(KafkaTopics.StockReserved);
                rider.AddProducer<string, StockReservationFailedIntegrationEvent>(KafkaTopics.StockReservationFailed);
                rider.AddConsumer<OrderCreatedConsumer>();
                rider.AddConsumer<OrderConfirmedConsumer>();
                rider.AddConsumer<ReleaseStockReservationConsumer>();
                rider.UsingKafka((context, kafka) =>
                {
                    kafka.Host(KafkaConnection.GetBootstrapServers(builder.Configuration));
                    kafka.TopicEndpoint<OrderCreatedIntegrationEvent>(KafkaTopics.OrderCreated, "inventory-service", endpoint =>
                    {
                        endpoint.UseMessageRetry(retry => retry.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));
                        endpoint.ConfigureConsumer<OrderCreatedConsumer>(context);
                    });
                    kafka.TopicEndpoint<OrderConfirmedIntegrationEvent>(KafkaTopics.OrderConfirmed, "inventory-service", endpoint =>
                    {
                        endpoint.UseMessageRetry(retry => retry.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));
                        endpoint.ConfigureConsumer<OrderConfirmedConsumer>(context);
                    });
                    kafka.TopicEndpoint<ReleaseStockReservationIntegrationEvent>(KafkaTopics.ReleaseStockReservation, "inventory-service", endpoint =>
                    {
                        endpoint.UseMessageRetry(retry => retry.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));
                        endpoint.ConfigureConsumer<ReleaseStockReservationConsumer>(context);
                    });
                });
            });
        });

        return builder;
    }
}
