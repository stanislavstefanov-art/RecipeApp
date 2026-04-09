using MediatR;

namespace Recipes.Application.Recipes.AddIngredientToRecipe;

public sealed record AddIngredientToRecipeCommand(
    Guid RecipeId,
    string Name,
    decimal Quantity,
    string Unit) : IRequest<AddIngredientToRecipeResult>;

public enum AddIngredientToRecipeResult
{
    Added = 0,
    NotFound = 1
}

