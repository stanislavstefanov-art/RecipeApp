using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.UpdateIngredientInRecipe;

public sealed record UpdateIngredientInRecipeCommand(
    Guid RecipeId,
    Guid IngredientId,
    string Name,
    decimal Quantity,
    string Unit) : IRequest<ErrorOr<Success>>;
