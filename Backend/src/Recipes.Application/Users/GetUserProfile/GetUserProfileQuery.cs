using ErrorOr;
using MediatR;

namespace Recipes.Application.Users.GetUserProfile;

public sealed record GetUserProfileQuery : IRequest<ErrorOr<UserProfileDto>>;

public sealed record UserProfileDto(Guid UserId, string Email, string DisplayName, Guid? PersonId);
