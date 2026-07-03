using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ECommerce.Notification.Data;

public sealed class NotificationDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
{
    public NotificationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseNpgsql(GetConnectionString())
            .Options;

        return new NotificationDbContext(options);
    }

    private static string GetConnectionString() =>
        Environment.GetEnvironmentVariable("ConnectionStrings__notificationdb")
        ?? "Host=localhost;Port=5432;Database=notificationdb;Username=postgres;Password=postgres";
}
