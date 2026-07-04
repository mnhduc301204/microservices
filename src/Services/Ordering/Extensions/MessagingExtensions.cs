using ECommerce.Contracts;
using ECommerce.Contracts.Basket;
using ECommerce.Contracts.Inventory;
using ECommerce.Contracts.Ordering;
using ECommerce.Contracts.Payment;
using ECommerce.Ordering.Consumers;
using ECommerce.Ordering.Data;
using ECommerce.Ordering.Features.CreateOrder;
using ECommerce.ServiceDefaults.Messaging;
using MassTransit;

namespace ECommerce.Ordering.Extensions;

public static class MessagingExtensions
{
    public static IHostApplicationBuilder AddOrderingMessaging(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<KafkaOutboxPublisher>();
        builder.Services.AddHostedService<OutboxDispatcher<OrderingDbContext>>();
        builder.Services.AddMediator(cfg =>
        {
            cfg.AddConsumers(typeof(Program).Assembly);
            cfg.AddRequestClient<CreateOrderCommand>();
        });
        builder.Services.AddMassTransit(bus =>
        {
            bus.AddConsumer<BasketCheckedOutConsumer>();
            bus.AddConsumer<PaymentSucceededConsumer>();
            bus.AddConsumer<PaymentFailedConsumer>();
            bus.AddConsumer<StockReservedConsumer>();
            bus.AddConsumer<StockReservationFailedConsumer>();
            bus.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
            bus.AddRider(rider =>
            {
                rider.AddProducer<OrderCreatedIntegrationEvent>(KafkaTopics.OrderCreated);
                rider.AddProducer<OrderConfirmedIntegrationEvent>(KafkaTopics.OrderConfirmed);
                rider.AddProducer<OrderCancelledIntegrationEvent>(KafkaTopics.OrderCancelled);
                rider.AddProducer<ReleaseStockReservationIntegrationEvent>(KafkaTopics.ReleaseStockReservation);
                rider.AddProducer<string, OrderCreatedIntegrationEvent>(KafkaTopics.OrderCreated);
                rider.AddProducer<string, OrderConfirmedIntegrationEvent>(KafkaTopics.OrderConfirmed);
                rider.AddProducer<string, OrderCancelledIntegrationEvent>(KafkaTopics.OrderCancelled);
                rider.AddProducer<string, ReleaseStockReservationIntegrationEvent>(KafkaTopics.ReleaseStockReservation);
                rider.AddConsumer<BasketCheckedOutConsumer>();
                rider.AddConsumer<PaymentSucceededConsumer>();
                rider.AddConsumer<PaymentFailedConsumer>();
                rider.AddConsumer<StockReservedConsumer>();
                rider.AddConsumer<StockReservationFailedConsumer>();
                rider.UsingKafka((context, kafka) =>
                {
                    kafka.Host(KafkaConnection.GetBootstrapServers(builder.Configuration));
                    kafka.TopicEndpoint<BasketCheckedOutIntegrationEvent>(KafkaTopics.BasketCheckedOut, "ordering-service", endpoint =>
                    {
                        endpoint.UseMessageRetry(retry => retry.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));
                        endpoint.ConfigureConsumer<BasketCheckedOutConsumer>(context);
                    });
                    kafka.TopicEndpoint<PaymentSucceededIntegrationEvent>(KafkaTopics.PaymentSucceeded, "ordering-service", endpoint =>
                    {
                        endpoint.UseMessageRetry(retry => retry.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));
                        endpoint.ConfigureConsumer<PaymentSucceededConsumer>(context);
                    });
                    kafka.TopicEndpoint<PaymentFailedIntegrationEvent>(KafkaTopics.PaymentFailed, "ordering-service", endpoint =>
                    {
                        endpoint.UseMessageRetry(retry => retry.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));
                        endpoint.ConfigureConsumer<PaymentFailedConsumer>(context);
                    });
                    kafka.TopicEndpoint<StockReservedIntegrationEvent>(KafkaTopics.StockReserved, "ordering-service", endpoint =>
                    {
                        endpoint.UseMessageRetry(retry => retry.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));
                        endpoint.ConfigureConsumer<StockReservedConsumer>(context);
                    });
                    kafka.TopicEndpoint<StockReservationFailedIntegrationEvent>(KafkaTopics.StockReservationFailed, "ordering-service", endpoint =>
                    {
                        endpoint.UseMessageRetry(retry => retry.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));
                        endpoint.ConfigureConsumer<StockReservationFailedConsumer>(context);
                    });
                });
            });
        });

        return builder;
    }
}
