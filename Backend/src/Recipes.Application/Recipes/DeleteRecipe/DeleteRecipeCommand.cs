using MediatR;

namespace Recipes.Application.Recipes.DeleteRecipe;

public sealed record DeleteRecipeCommand(Guid Id) : IRequest<DeleteRecipeResult>;

public enum DeleteRecipeResult
{
    Deleted = 0,
    NotFound = 1
}

