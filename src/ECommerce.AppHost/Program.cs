using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

var builder = DistributedApplication.CreateBuilder(args);

if (OperatingSystem.IsWindows())
{
    builder.Services.RemoveAll<ILoggerProvider>();
    builder.Services.AddLogging(logging => logging.AddConsole());
}

var postgres = builder.AddPostgres("postgres");
var catalogDb = postgres.AddDatabase("catalogdb");
var inventoryDb = postgres.AddDatabase("inventorydb");
var orderingDb = postgres.AddDatabase("orderingdb");
var paymentDb = postgres.AddDatabase("paymentdb");
var notificationDb = postgres.AddDatabase("notificationdb");

var basketRedis = builder.AddRedis("basket-redis");
var kafka = builder.AddKafka("kafka")
    .WithEnvironment("KAFKA_CFG_NUM_PARTITIONS", "12")
    .WithEnvironment("KAFKA_CFG_DEFAULT_REPLICATION_FACTOR", "1");

var catalog = builder.AddProject<Projects.Catalog>("catalog-api")
    .WithReference(catalogDb)
    .WaitFor(catalogDb)
    .WithReference(kafka)
    .WaitFor(kafka);

var inventory = builder.AddProject<Projects.Inventory>("inventory-api")
    .WithReference(inventoryDb)
    .WaitFor(inventoryDb)
    .WithReference(kafka)
    .WaitFor(kafka);

var basket = builder.AddProject<Projects.Basket>("basket-api")
    .WithReference(basketRedis)
    .WaitFor(basketRedis)
    .WithReference(kafka)
    .WaitFor(kafka)
    .WithReference(catalog)
    .WaitFor(catalog);

var ordering = builder.AddProject<Projects.Ordering>("ordering-api")
    .WithReference(orderingDb)
    .WaitFor(orderingDb)
    .WithReference(kafka)
    .WaitFor(kafka)
    .WithReference(basket)
    .WaitFor(basket);

var payment = builder.AddProject<Projects.Payment>("payment-api")
    .WithReference(paymentDb)
    .WaitFor(paymentDb)
    .WithReference(kafka)
    .WaitFor(kafka);

var notification = builder.AddProject<Projects.Notification>("notification-api")
    .WithReference(notificationDb)
    .WaitFor(notificationDb)
    .WithReference(kafka)
    .WaitFor(kafka)
    .WithReference(ordering)
    .WithReference(payment)
    .WithReference(inventory);

builder.AddProject<Projects.ECommerce_Gateway>("gateway")
    .WithReference(catalog)
    .WaitFor(catalog)
    .WithReference(basket)
    .WaitFor(basket)
    .WithReference(ordering)
    .WaitFor(ordering)
    .WithReference(payment)
    .WaitFor(payment)
    .WithReference(inventory)
    .WithReference(notification);

builder.Build().Run();
