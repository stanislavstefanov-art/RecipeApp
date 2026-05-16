using ErrorOr;
using MediatR;

namespace Recipes.Application.Users.ClearUserData;

public sealed record ClearUserDataCommand : IRequest<ErrorOr<Deleted>>;
