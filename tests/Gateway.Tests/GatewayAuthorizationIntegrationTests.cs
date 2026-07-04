using System.Net;
using System.Net.Http.Json;
using ECommerce.Gateway.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECommerce.Gateway.Tests;

public sealed class GatewayAuthorizationIntegrationTests
{
    [Fact]
    public async Task Gateway_WhenCustomerRouteHasNoToken_ReturnsUnauthorized()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/basket/test");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Gateway_WhenCustomerCallsAdminRoute_ReturnsForbidden()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var token = await IssueToken(client, "customer-1", "Customer");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/inventory/items");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Gateway_WhenAdminCallsAdminRoute_PassesAuthorizationLayer()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var token = await IssueToken(client, "admin-1", "Admin");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/inventory/items");

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DevelopmentTokenEndpoint_DoesNotReturnSigningKeyOrSecrets()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/dev-token", new DevTokenRequest("customer-1", "Customer"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("accessToken");
        body.Should().NotContain("signing-key");
        body.Should().NotContain("secret");
    }

    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Development");
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authentication:SigningKey"] = "test-signing-key-with-enough-length",
                        ["ReverseProxy:Clusters:inventory:Destinations:inventory-api:Address"] = "http://127.0.0.1:1",
                        ["ReverseProxy:Clusters:basket:Destinations:basket-api:Address"] = "http://127.0.0.1:1",
                    });
                });
            });

    private static async Task<string> IssueToken(HttpClient client, string subject, string role)
    {
        var response = await client.PostAsJsonAsync("/auth/dev-token", new DevTokenRequest(subject, role));
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<DevTokenResponse>();
        return token?.AccessToken ?? throw new InvalidOperationException("Token response was empty.");
    }
}
