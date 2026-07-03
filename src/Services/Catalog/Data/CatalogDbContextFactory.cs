using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ECommerce.Catalog.Data;

public sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(GetConnectionString())
            .Options;

        return new CatalogDbContext(options);
    }

    private static string GetConnectionString() =>
        Environment.GetEnvironmentVariable("ConnectionStrings__catalogdb")
        ?? "Host=localhost;Port=5432;Database=catalogdb;Username=postgres;Password=postgres";
}
