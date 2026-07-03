using Microsoft.AspNetCore.Http;

namespace ECommerce.ServiceDefaults.Correlation;

public sealed class CorrelationMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext httpContext, CorrelationContext correlationContext)
    {
        var correlationId = httpContext.Request.Headers.TryGetValue(CorrelationContext.HeaderName, out var headerValues)
            && !string.IsNullOrWhiteSpace(headerValues.FirstOrDefault())
            ? headerValues.First()!
            : Guid.NewGuid().ToString("N");

        correlationContext.CorrelationId = correlationId;
        httpContext.Response.Headers[CorrelationContext.HeaderName] = correlationId;

        await next(httpContext);
    }
}
