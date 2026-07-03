using ECommerce.Contracts;
using ECommerce.Contracts.Inventory;
using ECommerce.Contracts.Ordering;
using ECommerce.Contracts.Payment;
using ECommerce.Notification.Consumers;
using ECommerce.Notification.Data;
using ECommerce.ServiceDefaults.Messaging;
using MassTransit;

namespace ECommerce.Notification.Extensions;

public static class MessagingExtensions
{
    public static IHostApplicationBuilder AddNotificationMessaging(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<KafkaOutboxPublisher>();
        builder.Services.AddHostedService<OutboxDispatcher<NotificationDbContext>>();
        builder.Services.AddMediator(cfg => cfg.AddConsumers(typeof(Program).Assembly));
        builder.Services.AddMassTransit(bus =>
        {
            bus.AddConsumers(typeof(Program).Assembly);
            bus.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
            bus.AddRider(rider =>
            {
                rider.AddConsumers(typeof(Program).Assembly);
                rider.UsingKafka((context, kafka) =>
                {
                    kafka.Host(KafkaConnection.GetBootstrapServers(builder.Configuration));
                    kafka.TopicEndpoint<OrderCreatedIntegrationEvent>(KafkaTopics.OrderCreated, "notification-service", endpoint =>
                    {
                        endpoint.UseMessageRetry(retry => retry.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));
                        endpoint.ConfigureConsumer<OrderCreatedNotificationConsumer>(context);
                    });
                    kafka.TopicEndpoint<OrderConfirmedIntegrationEvent>(KafkaTopics.OrderConfirmed, "notification-service", endpoint =>
                    {
                        endpoint.UseMessageRetry(retry => retry.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));
                        endpoint.ConfigureConsumer<OrderConfirmedNotificationConsumer>(context);
                    });
                    kafka.TopicEndpoint<OrderCancelledIntegrationEvent>(KafkaTopics.OrderCancelled, "notification-service", endpoint =>
                    {
                        endpoint.UseMessageRetry(retry => retry.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));
                        endpoint.ConfigureConsumer<OrderCancelledNotificationConsumer>(context);
                    });
                    kafka.TopicEndpoint<StockReservationFailedIntegrationEvent>(KafkaTopics.StockReservationFailed, "notification-service", endpoint =>
                    {
                        endpoint.UseMessageRetry(retry => retry.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));
                        endpoint.ConfigureConsumer<StockReservationFailedNotificationConsumer>(context);
                    });
                    kafka.TopicEndpoint<PaymentSucceededIntegrationEvent>(KafkaTopics.PaymentSucceeded, "notification-service", endpoint =>
                    {
                        endpoint.UseMessageRetry(retry => retry.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));
                        endpoint.ConfigureConsumer<PaymentSucceededNotificationConsumer>(context);
                    });
                    kafka.TopicEndpoint<PaymentFailedIntegrationEvent>(KafkaTopics.PaymentFailed, "notification-service", endpoint =>
                    {
                        endpoint.UseMessageRetry(retry => retry.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));
                        endpoint.ConfigureConsumer<PaymentFailedNotificationConsumer>(context);
                    });
                });
            });
        });

        return builder;
    }
}
