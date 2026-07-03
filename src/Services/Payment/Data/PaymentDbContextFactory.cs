using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ECommerce.Payment.Data;

public sealed class PaymentDbContextFactory : IDesignTimeDbContextFactory<PaymentDbContext>
{
    public PaymentDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseNpgsql(GetConnectionString())
            .Options;

        return new PaymentDbContext(options);
    }

    private static string GetConnectionString() =>
        Environment.GetEnvironmentVariable("ConnectionStrings__paymentdb")
        ?? "Host=localhost;Port=5432;Database=paymentdb;Username=postgres;Password=postgres";
}
