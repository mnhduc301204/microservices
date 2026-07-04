using ECommerce.Catalog.Data;
using ECommerce.Catalog.Features.UpdateProduct;
using ECommerce.Catalog.Models;
using ECommerce.ServiceDefaults.Messaging;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Tests;

public sealed class UpdateProductEdgeTests
{
    [Fact]
    public async Task UpdateProduct_WhenProductDoesNotExist_ReturnsNotFoundAndDoesNotCreateOutbox()
    {
        await using var dbContext = CreateDbContext();
        var command = new UpdateProductCommand(
            Guid.NewGuid(),
            "Bottle",
            10m,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null);

        var result = await new UpdateProductHandler(dbContext).Handle(command, CancellationToken.None);

        result.GetType().Name.Should().Contain("NotFound");
        (await dbContext.Products.CountAsync()).Should().Be(0);
        (await dbContext.Set<OutboxMessage>().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task UpdateProduct_WhenValidationFails_DoesNotMutateExistingProduct()
    {
        await using var dbContext = CreateDbContext();
        var product = new Product("Bottle", "SKU-1", 10m, Guid.NewGuid(), Guid.NewGuid(), "Original");
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        var result = await new UpdateProductHandler(dbContext).Handle(
            new UpdateProductCommand(product.Id, "", -1m, Guid.Empty, Guid.Empty, new string('D', 2001)),
            CancellationToken.None);

        result.GetType().Name.Should().Contain("Problem");
        var unchanged = await dbContext.Products.SingleAsync();
        unchanged.Name.Should().Be("Bottle");
        unchanged.ListPrice.Should().Be(10m);
        unchanged.Description.Should().Be("Original");
    }

    private static CatalogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CatalogDbContext(options);
    }
}
