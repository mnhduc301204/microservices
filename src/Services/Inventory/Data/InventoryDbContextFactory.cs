using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ECommerce.Inventory.Data;

public sealed class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(GetConnectionString())
            .Options;

        return new InventoryDbContext(options);
    }

    private static string GetConnectionString() =>
        Environment.GetEnvironmentVariable("ConnectionStrings__inventorydb")
        ?? "Host=localhost;Port=5432;Database=inventorydb;Username=postgres;Password=postgres";
}
