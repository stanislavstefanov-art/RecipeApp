using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Recipes.Application.Abstractions;
using Recipes.Application.Common.Auth;
using Recipes.Domain.Entities;

namespace Recipes.Application.Auth.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, ErrorOr<AuthResultDto>>
{
    private readonly IRecipesDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtIssuer _jwtIssuer;

    public RegisterCommandHandler(IRecipesDbContext db, IPasswordHasher hasher, IJwtIssuer jwtIssuer)
    {
        _db = db;
        _hasher = hasher;
        _jwtIssuer = jwtIssuer;
    }

    public async Task<ErrorOr<AuthResultDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.ToLowerInvariant();

        var exists = await _db.Users.AnyAsync(u => u.Email == email, cancellationToken);
        if (exists)
        {
            return Error.Conflict("EmailExists", "An account with this email already exists.");
        }

        var hash = _hasher.Hash(request.Password);
        var user = User.CreateLocal(email, hash, request.DisplayName);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        var (token, expiresAt) = _jwtIssuer.Issue(user);
        return new AuthResultDto(
            token,
            expiresAt,
            new AuthUserDto(user.Id.Value, user.Email, user.DisplayName, user.AuthProvider.ToString()));
    }
}
