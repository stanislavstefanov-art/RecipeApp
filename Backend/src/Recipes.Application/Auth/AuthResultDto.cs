namespace Recipes.Application.Auth;

public sealed record AuthResultDto(
    string Token,
    DateTimeOffset ExpiresAt,
    AuthUserDto User);

public sealed record AuthUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Provider);
