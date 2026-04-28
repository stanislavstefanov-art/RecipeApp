using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Recipes.Application.Abstractions;
using Recipes.Application.Common.Auth;
using Recipes.Domain.Entities;
using Recipes.Domain.Enums;

namespace Recipes.Application.Auth.EntraExchange;

public sealed class EntraExchangeCommandHandler : IRequestHandler<EntraExchangeCommand, ErrorOr<AuthResultDto>>
{
    private readonly IRecipesDbContext _db;
    private readonly IEntraTokenValidator _validator;
    private readonly IJwtIssuer _jwtIssuer;

    public EntraExchangeCommandHandler(
        IRecipesDbContext db,
        IEntraTokenValidator validator,
        IJwtIssuer jwtIssuer)
    {
        _db = db;
        _validator = validator;
        _jwtIssuer = jwtIssuer;
    }

    public async Task<ErrorOr<AuthResultDto>> Handle(
        EntraExchangeCommand request,
        CancellationToken cancellationToken)
    {
        var identity = await _validator.ValidateAsync(request.IdToken, cancellationToken);
        if (identity is null)
            return Error.Unauthorized("InvalidCredentials", "The supplied Entra id_token is invalid.");

        // 1. Lookup by EntraObjectId
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.EntraObjectId == identity.ObjectId, cancellationToken);

        if (user is not null)
        {
            user.RecordLogin(DateTimeOffset.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            return IssueResult(user);
        }

        // 2. Fallback lookup by email
        var email = identity.Email.ToLowerInvariant();
        var byEmail = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (byEmail is not null)
        {
            if (byEmail.AuthProvider == AuthProvider.Local)
            {
                return Error.Conflict(
                    "AuthProviderMismatch",
                    "This email is registered with a password — log in that way instead.");
            }

            // Edge case: an Entra user with a different OID but same email. Update the OID.
            byEmail.RecordLogin(DateTimeOffset.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            return IssueResult(byEmail);
        }

        // 3. First-time Entra login — create user
        var newUser = User.CreateEntra(email, identity.ObjectId, identity.DisplayName);
        _db.Users.Add(newUser);
        await _db.SaveChangesAsync(cancellationToken);
        return IssueResult(newUser);
    }

    private AuthResultDto IssueResult(User user)
    {
        var (token, expiresAt) = _jwtIssuer.Issue(user);
        return new AuthResultDto(
            token,
            expiresAt,
            new AuthUserDto(user.Id.Value, user.Email, user.DisplayName, user.AuthProvider.ToString()));
    }
}
