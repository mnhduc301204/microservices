using ECommerce.Inventory.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Inventory.Extensions;

public static class ServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddInventoryData(this IHostApplicationBuilder builder)
    {
        if (builder.Configuration.GetConnectionString("inventorydb") is not null)
        {
            builder.AddNpgsqlDbContext<InventoryDbContext>("inventorydb");
        }
        else
        {
            builder.Services.AddDbContext<InventoryDbContext>(options => options.UseInMemoryDatabase("inventorydb"));
        }

        return builder;
    }
}
