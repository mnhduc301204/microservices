using ECommerce.Catalog.Data;
using ECommerce.Catalog.Features.SearchProducts;
using ECommerce.Catalog.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Tests;

public sealed class SearchProductsEdgeTests
{
    [Fact]
    public async Task SearchProducts_WhenPageNumberAndSizeAreInvalid_ClampsToSupportedRange()
    {
        await using var dbContext = CreateDbContext();
        SeedProducts(dbContext, 150);
        await dbContext.SaveChangesAsync();

        var result = await new SearchProductsHandler(dbContext).Handle(
            new SearchProductsQuery(null, null, PageNumber: -10, PageSize: 500),
            CancellationToken.None);

        var response = result.Should().BeOfType<Ok<SearchProductsResponse>>().Subject.Value!;
        response.PageNumber.Should().Be(1);
        response.PageSize.Should().Be(100);
        response.TotalCount.Should().Be(150);
        response.Items.Should().HaveCount(100);
    }

    [Fact]
    public async Task SearchProducts_WhenStatusFilterIsUsed_ReturnsOnlyMatchingStatus()
    {
        await using var dbContext = CreateDbContext();
        var active = new Product("Active Product", "active-1", 10m, Guid.NewGuid(), Guid.NewGuid(), null);
        active.ChangeStatus(ProductStatus.Active);
        var archived = new Product("Archived Product", "archived-1", 10m, Guid.NewGuid(), Guid.NewGuid(), null);
        archived.ChangeStatus(ProductStatus.Archived);
        dbContext.Products.AddRange(active, archived);
        await dbContext.SaveChangesAsync();

        var result = await new SearchProductsHandler(dbContext).Handle(
            new SearchProductsQuery(null, ProductStatus.Active),
            CancellationToken.None);

        var response = result.Should().BeOfType<Ok<SearchProductsResponse>>().Subject.Value!;
        response.Items.Should().ContainSingle();
        response.Items.Single().Status.Should().Be(ProductStatus.Active);
    }

    [Theory]
    [InlineData("Bottle", 1)]
    [InlineData("SKU-002", 1)]
    [InlineData("missing", 0)]
    [InlineData("   ", 2)]
    public async Task SearchProducts_SearchTerm_FiltersByNameOrSkuAndIgnoresWhitespaceOnly(string? searchTerm, int expectedCount)
    {
        await using var dbContext = CreateDbContext();
        dbContext.Products.AddRange(
            new Product("Bottle", "SKU-001", 10m, Guid.NewGuid(), Guid.NewGuid(), null),
            new Product("Backpack", "SKU-002", 20m, Guid.NewGuid(), Guid.NewGuid(), null));
        await dbContext.SaveChangesAsync();

        var result = await new SearchProductsHandler(dbContext).Handle(
            new SearchProductsQuery(searchTerm, null),
            CancellationToken.None);

        var response = result.Should().BeOfType<Ok<SearchProductsResponse>>().Subject.Value!;
        response.Items.Should().HaveCount(expectedCount);
    }

    private static CatalogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CatalogDbContext(options);
    }

    private static void SeedProducts(CatalogDbContext dbContext, int count)
    {
        for (var index = 0; index < count; index++)
        {
            dbContext.Products.Add(new Product(
                $"Product {index:000}",
                $"SKU-{index:000}",
                index,
                Guid.NewGuid(),
                Guid.NewGuid(),
                null));
        }
    }
}
