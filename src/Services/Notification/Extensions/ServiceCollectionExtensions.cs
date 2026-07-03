using ECommerce.Notification.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Notification.Extensions;

public static class ServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddNotificationData(this IHostApplicationBuilder builder)
    {
        if (builder.Configuration.GetConnectionString("notificationdb") is not null)
        {
            builder.AddNpgsqlDbContext<NotificationDbContext>("notificationdb");
        }
        else
        {
            builder.Services.AddDbContext<NotificationDbContext>(options => options.UseInMemoryDatabase("notificationdb"));
        }

        return builder;
    }
}
