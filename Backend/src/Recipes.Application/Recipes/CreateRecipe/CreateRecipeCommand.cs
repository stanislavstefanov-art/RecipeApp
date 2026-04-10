using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.CreateRecipe;

public sealed record CreateRecipeCommand(string Name) : IRequest<ErrorOr<CreateRecipeResponse>>;

public sealed record CreateRecipeResponse(Guid Id);
