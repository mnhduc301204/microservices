using ECommerce.Inventory.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace ECommerce.Infrastructure.Tests;

public sealed class PostgresMigrationTests
{
    [Fact]
    public async Task InventorySchema_Migrates_OnPostgres()
    {
        PostgreSqlContainer postgres;
        try
        {
            postgres = new PostgreSqlBuilder()
                .WithImage("postgres:17-alpine")
                .WithDatabase("inventory_test")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();
            await postgres.StartAsync();
        }
        catch (Exception ex) when (ex.Message.Contains("Docker", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await using (postgres)
        {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(postgres.GetConnectionString())
            .Options;

        await using var dbContext = new InventoryDbContext(options);
        await dbContext.Database.MigrateAsync();

        (await dbContext.Database.CanConnectAsync()).Should().BeTrue();
        }
    }
}
