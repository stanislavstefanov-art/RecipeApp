namespace Recipes.Domain.Entities;

using Recipes.Domain.Enums;
using Recipes.Domain.Primitives;

public sealed class User : Entity
{
    public UserId Id { get; private set; } = UserId.New();
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public AuthProvider AuthProvider { get; private set; }
    public string? PasswordHash { get; private set; }
    public Guid? EntraObjectId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }

    private User() { }

    public static User CreateLocal(string email, string passwordHash, string displayName)
    {
        return new User
        {
            Email = email.ToLowerInvariant(),
            DisplayName = displayName,
            AuthProvider = AuthProvider.Local,
            PasswordHash = passwordHash,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public static User CreateEntra(string email, Guid objectId, string displayName)
    {
        return new User
        {
            Email = email.ToLowerInvariant(),
            DisplayName = displayName,
            AuthProvider = AuthProvider.Entra,
            EntraObjectId = objectId,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void RecordLogin(DateTimeOffset now)
    {
        LastLoginAt = now;
    }
}
