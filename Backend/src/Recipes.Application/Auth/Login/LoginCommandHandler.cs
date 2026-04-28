using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Recipes.Application.Abstractions;
using Recipes.Application.Common.Auth;
using Recipes.Domain.Enums;

namespace Recipes.Application.Auth.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, ErrorOr<AuthResultDto>>
{
    private static readonly Error InvalidCredentials =
        Error.Unauthorized("InvalidCredentials", "Invalid email or password.");

    private readonly IRecipesDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtIssuer _jwtIssuer;

    public LoginCommandHandler(IRecipesDbContext db, IPasswordHasher hasher, IJwtIssuer jwtIssuer)
    {
        _db = db;
        _hasher = hasher;
        _jwtIssuer = jwtIssuer;
    }

    public async Task<ErrorOr<AuthResultDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null || user.AuthProvider != AuthProvider.Local || user.PasswordHash is null)
        {
            return InvalidCredentials;
        }

        if (!_hasher.Verify(request.Password, user.PasswordHash))
        {
            return InvalidCredentials;
        }

        user.RecordLogin(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);

        var (token, expiresAt) = _jwtIssuer.Issue(user);
        return new AuthResultDto(
            token,
            expiresAt,
            new AuthUserDto(user.Id.Value, user.Email, user.DisplayName, user.AuthProvider.ToString()));
    }
}
