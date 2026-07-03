using ECommerce.Gateway.Auth;
using ECommerce.ServiceDefaults;
using ECommerce.ServiceDefaults.Security;
using System.Threading.RateLimiting;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddGatewayAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 120,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1),
            }));
});
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver()
    .AddTransforms(transformBuilderContext =>
    {
        transformBuilderContext.AddRequestTransform(transformContext =>
        {
            var internalApiKey = builder.Configuration["InternalApi:ApiKey"];
            if (!string.IsNullOrWhiteSpace(internalApiKey))
            {
                transformContext.ProxyRequest.Headers.Remove(InternalApiKeyMiddleware.HeaderName);
                transformContext.ProxyRequest.Headers.TryAddWithoutValidation(InternalApiKeyMiddleware.HeaderName, internalApiKey);
            }

            return ValueTask.CompletedTask;
        });
    });

var app = builder.Build();

app.UseExceptionHandler();
app.UseRateLimiter();
app.MapDefaultEndpoints();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment())
{
    app.MapDevelopmentTokenEndpoint();
}

app.MapReverseProxy();

app.Run();

public partial class Program;
