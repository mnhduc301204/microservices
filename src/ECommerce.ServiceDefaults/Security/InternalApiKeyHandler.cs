using Microsoft.Extensions.Configuration;

namespace ECommerce.ServiceDefaults.Security;

public sealed class InternalApiKeyHandler(IConfiguration configuration) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var configuredKey = configuration["InternalApi:ApiKey"];
        if (!string.IsNullOrWhiteSpace(configuredKey))
        {
            request.Headers.Remove(InternalApiKeyMiddleware.HeaderName);
            request.Headers.Add(InternalApiKeyMiddleware.HeaderName, configuredKey);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
