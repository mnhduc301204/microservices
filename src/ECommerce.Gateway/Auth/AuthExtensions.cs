using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.Gateway.Auth;

public static class AuthExtensions
{
    public static IServiceCollection AddGatewayAuthentication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var issuer = configuration["Authentication:Issuer"] ?? "ecommerce-dev";
        var audience = configuration["Authentication:Audience"] ?? "ecommerce-api";
        var signingKey = configuration["Authentication:SigningKey"];

        if (string.IsNullOrWhiteSpace(signingKey) && environment.IsDevelopment())
        {
            signingKey = "development-only-signing-key-change-before-production";
        }

        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException("Authentication:SigningKey must be configured.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));

        services.AddSingleton(new JwtTokenIssuer(issuer, audience, key));
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.NameIdentifier,
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("Customer", policy => policy.RequireAuthenticatedUser().RequireRole("Customer", "Admin"));
            options.AddPolicy("Admin", policy => policy.RequireAuthenticatedUser().RequireRole("Admin"));
            options.AddPolicy("Internal", policy => policy.RequireAuthenticatedUser().RequireRole("Internal", "Admin"));
        });

        return services;
    }

    public static IEndpointRouteBuilder MapDevelopmentTokenEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/auth/dev-token", (DevTokenRequest request, JwtTokenIssuer issuer) =>
        {
            var role = string.Equals(request.Role, "Admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "Customer";
            return Results.Ok(issuer.CreateToken(request.Subject, role));
        })
        .AllowAnonymous();

        return endpoints;
    }
}

public sealed class JwtTokenIssuer(string issuer, string audience, SecurityKey signingKey)
{
    public DevTokenResponse CreateToken(string subject, string role)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            subject = "dev-user";
        }

        var expiresAt = DateTimeOffset.UtcNow.AddHours(2);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            [
                new Claim(ClaimTypes.NameIdentifier, subject.Trim()),
                new Claim(ClaimTypes.Role, role),
            ],
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new DevTokenResponse(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
