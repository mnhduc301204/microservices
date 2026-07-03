using ECommerce.Contracts;
using ECommerce.Contracts.Inventory;
using ECommerce.Contracts.Payment;
using ECommerce.Payment.Consumers;
using ECommerce.Payment.Data;
using ECommerce.ServiceDefaults.Messaging;
using MassTransit;

namespace ECommerce.Payment.Extensions;

public static class MessagingExtensions
{
    public static IHostApplicationBuilder AddPaymentMessaging(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<KafkaOutboxPublisher>();
        builder.Services.AddHostedService<OutboxDispatcher<PaymentDbContext>>();
        builder.Services.AddMediator(cfg => cfg.AddConsumers(typeof(Program).Assembly));
        builder.Services.AddMassTransit(bus =>
        {
            bus.AddConsumer<StockReservedConsumer>();
            bus.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
            bus.AddRider(rider =>
            {
                rider.AddProducer<PaymentSucceededIntegrationEvent>(KafkaTopics.PaymentSucceeded);
                rider.AddProducer<PaymentFailedIntegrationEvent>(KafkaTopics.PaymentFailed);
                rider.AddConsumer<StockReservedConsumer>();
                rider.UsingKafka((context, kafka) =>
                {
                    kafka.Host(KafkaConnection.GetBootstrapServers(builder.Configuration));
                    kafka.TopicEndpoint<StockReservedIntegrationEvent>(KafkaTopics.StockReserved, "payment-service", endpoint =>
                    {
                        endpoint.UseMessageRetry(retry => retry.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));
                        endpoint.ConfigureConsumer<StockReservedConsumer>(context);
                    });
                });
            });
        });

        return builder;
    }
}
