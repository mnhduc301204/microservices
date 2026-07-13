using ECommerce.Payment.BackgroundServices;
using ECommerce.Payment.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Payment.Extensions;

public static class ServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddPaymentData(this IHostApplicationBuilder builder)
    {
        if (builder.Configuration.GetConnectionString("paymentdb") is not null)
        {
            builder.AddNpgsqlDbContext<PaymentDbContext>("paymentdb");
        }
        else
        {
            builder.Services.AddDbContext<PaymentDbContext>(options => options.UseInMemoryDatabase("paymentdb"));
        }

        builder.Services.AddHostedService<FakePaymentProviderWorker>();
        builder.Services.AddHostedService<PaymentOutboxRepairService>();

        return builder;
    }
}
