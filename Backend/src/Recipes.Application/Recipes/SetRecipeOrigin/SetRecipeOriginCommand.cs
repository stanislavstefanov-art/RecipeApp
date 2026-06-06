using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.SetRecipeOrigin;

public sealed record SetRecipeOriginCommand(Guid RecipeId, int Origin) : IRequest<ErrorOr<Success>>;
