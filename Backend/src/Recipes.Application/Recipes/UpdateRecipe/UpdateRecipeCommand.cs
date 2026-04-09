using MediatR;

namespace Recipes.Application.Recipes.UpdateRecipe;

public sealed record UpdateRecipeCommand(Guid Id, string Name) : IRequest<UpdateRecipeResult>;

public enum UpdateRecipeResult
{
    Updated = 0,
    NotFound = 1
}

