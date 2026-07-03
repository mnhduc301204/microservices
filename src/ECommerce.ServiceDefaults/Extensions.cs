using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ECommerce.ServiceDefaults.Correlation;
using ECommerce.ServiceDefaults.Security;

namespace ECommerce.ServiceDefaults;

public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        var requireInternalApiKey = builder.Configuration.GetValue("InternalApi:RequireInbound", true);
        if (!builder.Environment.IsDevelopment()
            && requireInternalApiKey
            && string.IsNullOrWhiteSpace(builder.Configuration["InternalApi:ApiKey"]))
        {
            throw new InvalidOperationException("InternalApi:ApiKey must be configured when InternalApi:RequireInbound is enabled.");
        }

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddHealthChecks();
        builder.Services.AddProblemDetails();
        builder.Services.AddScoped<CorrelationContext>();
        builder.Services.AddTransient<InternalApiKeyHandler>();
        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddServiceDiscovery();
            http.AddHttpMessageHandler<InternalApiKeyHandler>();
            http.AddStandardResilienceHandler();
        });

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.UseMiddleware<CorrelationMiddleware>();
        app.UseMiddleware<InternalApiKeyMiddleware>();
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = _ => false,
        });

        return app;
    }

    public static async Task<WebApplication> ApplyMigrations<TDbContext>(this WebApplication app)
        where TDbContext : DbContext
    {
        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        if (dbContext.Database.IsRelational())
        {
            await dbContext.Database.MigrateAsync();
        }

        return app;
    }
}
