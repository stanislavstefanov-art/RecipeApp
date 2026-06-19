using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Recipes.Application.Abstractions;
using Recipes.Application.Common;

namespace Recipes.Application.Users.GetUserProfile;

public sealed class GetUserProfileHandler : IRequestHandler<GetUserProfileQuery, ErrorOr<UserProfileDto>>
{
    private readonly IRecipesDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetUserProfileHandler(IRecipesDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<UserProfileDto>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

        if (user is null)
            return Error.NotFound("User.NotFound", "User not found.");

        return new UserProfileDto(
            user.Id.Value,
            user.Email,
            user.DisplayName,
            user.PersonId?.Value);
    }
}
