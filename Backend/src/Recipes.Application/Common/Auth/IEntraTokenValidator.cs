namespace Recipes.Application.Common.Auth;

public sealed record EntraIdentity(string Email, Guid ObjectId, string DisplayName);

public interface IEntraTokenValidator
{
    Task<EntraIdentity?> ValidateAsync(string idToken, CancellationToken ct);
}
