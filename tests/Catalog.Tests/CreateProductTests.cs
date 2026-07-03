using ECommerce.Catalog.Data;
using ECommerce.Catalog.Features.CreateProduct;
using ECommerce.Catalog.Models;
using ECommerce.Contracts;
using ECommerce.ServiceDefaults.Messaging;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Tests;

public sealed class CreateProductTests
{
    [Fact]
    public async Task CreateProduct_WithValidCommand_PersistsDraftProduct()
    {
        await using var dbContext = CreateDbContext();
        var command = new CreateProductCommand("Trail Shoe", "shoe-001", 129.99m, Guid.NewGuid(), Guid.NewGuid(), "Lightweight shoe");

        await new CreateProductHandler(dbContext).Handle(command, CancellationToken.None);

        var product = await dbContext.Products.SingleAsync();
        product.Name.Should().Be("Trail Shoe");
        product.Sku.Should().Be("SHOE-001");
        product.Status.Should().Be(ProductStatus.Draft);

        var outboxMessage = await dbContext.Set<OutboxMessage>().SingleAsync();
        outboxMessage.Topic.Should().Be(KafkaTopics.ProductCreated);
        outboxMessage.MessageType.Should().Contain("ProductCreatedIntegrationEvent");
    }

    private static CatalogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CatalogDbContext(options);
    }
}
