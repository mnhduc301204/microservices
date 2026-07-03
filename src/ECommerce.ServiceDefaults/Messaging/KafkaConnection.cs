using Microsoft.Extensions.Configuration;

namespace ECommerce.ServiceDefaults.Messaging;

public static class KafkaConnection
{
    public static string GetBootstrapServers(IConfiguration configuration)
    {
        return configuration.GetConnectionString("kafka")
            ?? configuration["Kafka:BootstrapServers"]
            ?? "localhost:9092";
    }
}
