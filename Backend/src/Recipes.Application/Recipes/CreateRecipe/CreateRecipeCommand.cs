using MediatR;

namespace Recipes.Application.Recipes.CreateRecipe;

public sealed record CreateRecipeCommand(string Name) : IRequest<CreateRecipeResponse>;

public sealed record CreateRecipeResponse(Guid Id);

