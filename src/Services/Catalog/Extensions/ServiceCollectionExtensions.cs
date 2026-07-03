using ECommerce.Catalog.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Extensions;

public static class ServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddCatalogData(this IHostApplicationBuilder builder)
    {
        if (builder.Configuration.GetConnectionString("catalogdb") is not null)
        {
            builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb");
        }
        else
        {
            builder.Services.AddDbContext<CatalogDbContext>(options => options.UseInMemoryDatabase("catalogdb"));
        }

        return builder;
    }
}
