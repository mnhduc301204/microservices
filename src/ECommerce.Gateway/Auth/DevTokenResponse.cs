namespace ECommerce.Gateway.Auth;

public sealed record DevTokenResponse(string AccessToken, DateTimeOffset ExpiresAt);
