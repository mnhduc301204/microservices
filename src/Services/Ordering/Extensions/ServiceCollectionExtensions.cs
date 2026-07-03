using ECommerce.Ordering.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Extensions;

public static class ServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddOrderingData(this IHostApplicationBuilder builder)
    {
        if (builder.Configuration.GetConnectionString("orderingdb") is not null)
        {
            builder.AddNpgsqlDbContext<OrderingDbContext>("orderingdb");
        }
        else
        {
            builder.Services.AddDbContext<OrderingDbContext>(options => options.UseInMemoryDatabase("orderingdb"));
        }

        return builder;
    }
}
