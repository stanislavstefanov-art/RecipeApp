using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Recipes.Application.Abstractions;
using Recipes.Application.Common;

namespace Recipes.Application.Auth.Me;

public sealed class MeQueryHandler : IRequestHandler<MeQuery, ErrorOr<MeDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IRecipesDbContext _db;

    public MeQueryHandler(ICurrentUser currentUser, IRecipesDbContext db)
    {
        _currentUser = currentUser;
        _db = db;
    }

    public async Task<ErrorOr<MeDto>> Handle(MeQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return Error.Unauthorized("AuthRequired", "User not found.");
        }

        var householdIds = await _currentUser.GetHouseholdIdsAsync(cancellationToken);

        var households = await _db.Households
            .Where(h => householdIds.Contains(h.Id))
            .Select(h => new HouseholdSummaryDto(h.Id.Value, h.Name))
            .ToListAsync(cancellationToken);

        var userDto = new AuthUserDto(user.Id.Value, user.Email, user.DisplayName, user.AuthProvider.ToString());
        return new MeDto(userDto, households);
    }
}
