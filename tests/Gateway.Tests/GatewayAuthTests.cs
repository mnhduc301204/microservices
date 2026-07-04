using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ECommerce.Gateway.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.Gateway.Tests;

public sealed class GatewayAuthTests
{
    [Fact]
    public void AddGatewayAuthentication_WhenProductionSigningKeyIsMissing_Throws()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        Action act = () => services.AddGatewayAuthentication(configuration, new TestHostEnvironment(Environments.Production));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Authentication:SigningKey*");
    }

    [Theory]
    [InlineData(" customer-1 ", "Customer", "customer-1", "Customer")]
    [InlineData("", "Admin", "dev-user", "Admin")]
    [InlineData("staff", "Internal", "staff", "Internal")]
    public void JwtTokenIssuer_CreateToken_IssuesExpectedSubjectAndRole(string subject, string role, string expectedSubject, string expectedRole)
    {
        var issuer = new JwtTokenIssuer(
            "test-issuer",
            "test-audience",
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-signing-key-with-enough-length")));

        var response = issuer.CreateToken(subject, role);

        response.AccessToken.Should().NotBeNullOrWhiteSpace();
        response.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken);
        token.Issuer.Should().Be("test-issuer");
        token.Audiences.Should().Contain("test-audience");
        token.Claims.Single(claim => claim.Type == ClaimTypes.NameIdentifier).Value.Should().Be(expectedSubject);
        token.Claims.Single(claim => claim.Type == ClaimTypes.Role).Value.Should().Be(expectedRole);
    }

    [Fact]
    public void AddGatewayAuthentication_WhenDevelopmentSigningKeyIsMissing_UsesDevelopmentFallback()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddGatewayAuthentication(configuration, new TestHostEnvironment(Environments.Development));

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<JwtTokenIssuer>().Should().NotBeNull();
    }

    [Theory]
    [InlineData("Customer", "Customer", true)]
    [InlineData("Admin", "Customer", true)]
    [InlineData("Internal", "Customer", false)]
    [InlineData("Customer", "Admin", false)]
    [InlineData("Admin", "Admin", true)]
    [InlineData("Internal", "Admin", false)]
    [InlineData("Internal", "Internal", true)]
    [InlineData("Admin", "Internal", true)]
    [InlineData("Customer", "Internal", false)]
    public async Task AuthorizationPolicies_EnforceExpectedRoles(string userRole, string policyName, bool expected)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:SigningKey"] = "test-signing-key-with-enough-length",
            })
            .Build();
        services.AddLogging();
        services.AddGatewayAuthentication(configuration, new TestHostEnvironment(Environments.Production));
        using var provider = services.BuildServiceProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "user-1"),
                new Claim(ClaimTypes.Role, userRole),
            ],
            "Test"));

        var result = await authorization.AuthorizeAsync(principal, resource: null, policyName);

        result.Succeeded.Should().Be(expected);
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Gateway.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
