using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Recipes.Application.Common.Auth;

namespace Recipes.Api.Auth;

public sealed class EntraTokenValidator : IEntraTokenValidator
{
    private readonly EntraOptions _options;
    private readonly IConfigurationManager<OpenIdConnectConfiguration> _configManager;

    public EntraTokenValidator(IOptions<EntraOptions> options)
    {
        _options = options.Value;
        var discoveryUrl =
            $"https://login.microsoftonline.com/{_options.TenantId}/v2.0/.well-known/openid-configuration";
        _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            discoveryUrl,
            new OpenIdConnectConfigurationRetriever());
    }

    public async Task<EntraIdentity?> ValidateAsync(string idToken, CancellationToken ct)
    {
        try
        {
            var config = await _configManager.GetConfigurationAsync(ct);

            var tokenHandler = new JsonWebTokenHandler();
            var result = await tokenHandler.ValidateTokenAsync(idToken, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = $"https://login.microsoftonline.com/{_options.TenantId}/v2.0",
                ValidateAudience = true,
                ValidAudience = _options.ClientId,
                ValidateLifetime = true,
                IssuerSigningKeys = config.SigningKeys,
                ValidateIssuerSigningKey = true,
            });

            if (!result.IsValid)
                return null;

            var claims = result.Claims;

            var oidStr = GetClaimString(claims, "oid")
                      ?? GetClaimString(claims,
                             "http://schemas.microsoft.com/identity/claims/objectidentifier");

            if (!Guid.TryParse(oidStr, out var oid))
                return null;

            var email = GetClaimString(claims, "email")
                     ?? GetClaimString(claims, "preferred_username")
                     ?? string.Empty;

            var displayName = GetClaimString(claims, "name") ?? email;

            return new EntraIdentity(email, oid, displayName);
        }
        catch
        {
            return null;
        }
    }

    private static string? GetClaimString(IDictionary<string, object> claims, string key)
        => claims.TryGetValue(key, out var value) ? value as string : null;
}
