using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.SetRecipeType;

public sealed record SetRecipeTypeCommand(Guid RecipeId, int RecipeType) : IRequest<ErrorOr<Success>>;
