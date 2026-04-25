namespace Recipes.Application.Recipes.ScaleRecipe;

public sealed record ScaledRecipeDto(
    Guid RecipeId,
    string Name,
    int FromServings,
    int ToServings,
    IReadOnlyList<ScaledIngredientDto> Ingredients,
    int AttemptsTaken);

public sealed record ScaledIngredientDto(
    string Name,
    decimal Quantity,
    string Unit);
