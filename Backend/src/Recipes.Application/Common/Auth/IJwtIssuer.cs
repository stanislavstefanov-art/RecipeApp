using Recipes.Domain.Entities;

namespace Recipes.Application.Common.Auth;

public interface IJwtIssuer
{
    (string Token, DateTimeOffset ExpiresAt) Issue(User user);
}
