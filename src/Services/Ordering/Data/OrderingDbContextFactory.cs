using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ECommerce.Ordering.Data;

public sealed class OrderingDbContextFactory : IDesignTimeDbContextFactory<OrderingDbContext>
{
    public OrderingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OrderingDbContext>()
            .UseNpgsql(GetConnectionString())
            .Options;

        return new OrderingDbContext(options);
    }

    private static string GetConnectionString() =>
        Environment.GetEnvironmentVariable("ConnectionStrings__orderingdb")
        ?? "Host=localhost;Port=5432;Database=orderingdb;Username=postgres;Password=postgres";
}
