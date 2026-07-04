using ECommerce.ServiceDefaults.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ECommerce.ServiceDefaults.Tests;

public sealed class InternalApiKeySecurityTests
{
    [Fact]
    public async Task InternalApiKeyMiddleware_WhenKeyIsMissing_ReturnsForbiddenAndDoesNotCallNext()
    {
        var called = false;
        var middleware = new InternalApiKeyMiddleware(
            _ =>
            {
                called = true;
                return Task.CompletedTask;
            },
            Configuration(requireInbound: true, apiKey: "secret-key"));
        var context = new DefaultHttpContext();

        await middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        called.Should().BeFalse();
    }

    [Fact]
    public async Task InternalApiKeyMiddleware_WhenKeyIsInvalid_ReturnsForbidden()
    {
        var middleware = new InternalApiKeyMiddleware(
            _ => Task.CompletedTask,
            Configuration(requireInbound: true, apiKey: "secret-key"));
        var context = new DefaultHttpContext();
        context.Request.Headers[InternalApiKeyMiddleware.HeaderName] = "wrong-key";

        await middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    [InlineData("/alive")]
    public async Task InternalApiKeyMiddleware_WhenInfrastructureEndpoint_SkipsKeyRequirement(string path)
    {
        var called = false;
        var middleware = new InternalApiKeyMiddleware(
            _ =>
            {
                called = true;
                return Task.CompletedTask;
            },
            Configuration(requireInbound: true, apiKey: "secret-key"));
        var context = new DefaultHttpContext();
        context.Request.Path = path;

        await middleware.Invoke(context);

        called.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InternalApiKeyMiddleware_WhenInboundRequirementDisabled_CallsNextWithoutHeader()
    {
        var called = false;
        var middleware = new InternalApiKeyMiddleware(
            _ =>
            {
                called = true;
                return Task.CompletedTask;
            },
            Configuration(requireInbound: false, apiKey: "secret-key"));

        await middleware.Invoke(new DefaultHttpContext());

        called.Should().BeTrue();
    }

    [Fact]
    public async Task InternalApiKeyHandler_AddsConfiguredHeaderAndReplacesExistingValue()
    {
        var inner = new CaptureHandler();
        using var invoker = new HttpMessageInvoker(new InternalApiKeyHandler(Configuration(requireInbound: true, apiKey: "secret-key"))
        {
            InnerHandler = inner,
        });
        var request = new HttpRequestMessage(HttpMethod.Get, "https://internal.test");
        request.Headers.Add(InternalApiKeyMiddleware.HeaderName, "old-key");

        await invoker.SendAsync(request, CancellationToken.None);

        inner.CapturedRequest.Should().NotBeNull();
        inner.CapturedRequest!.Headers.GetValues(InternalApiKeyMiddleware.HeaderName).Should().Equal("secret-key");
    }

    private static IConfiguration Configuration(bool requireInbound, string? apiKey) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InternalApi:RequireInbound"] = requireInbound.ToString(),
                ["InternalApi:ApiKey"] = apiKey,
            })
            .Build();

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
