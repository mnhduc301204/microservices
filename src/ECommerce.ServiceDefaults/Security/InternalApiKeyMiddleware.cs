using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace ECommerce.ServiceDefaults.Security;

public sealed class InternalApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public const string HeaderName = "X-Internal-Api-Key";

    public async Task Invoke(HttpContext httpContext)
    {
        var configuredKey = configuration["InternalApi:ApiKey"];
        var requireInbound = configuration.GetValue("InternalApi:RequireInbound", true);
        if (!requireInbound || string.IsNullOrWhiteSpace(configuredKey))
        {
            await next(httpContext);
            return;
        }

        if (IsInfrastructureEndpoint(httpContext.Request.Path))
        {
            await next(httpContext);
            return;
        }

        if (!httpContext.Request.Headers.TryGetValue(HeaderName, out var headerValue)
            || headerValue.Count == 0
            || headerValue[0] != configuredKey)
        {
            httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            await httpContext.Response.WriteAsJsonAsync(new { error = "Internal API key is invalid." });
            return;
        }

        await next(httpContext);
    }

    private static bool IsInfrastructureEndpoint(PathString path) =>
        path.StartsWithSegments("/health") || path.StartsWithSegments("/alive");
}
