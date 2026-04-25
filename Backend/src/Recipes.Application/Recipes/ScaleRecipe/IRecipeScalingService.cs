using Recipes.Application.Recipes.GetRecipe;

namespace Recipes.Application.Recipes.ScaleRecipe;

public interface IRecipeScalingService
{
    Task<ScaledRecipeDto> ScaleAsync(
        RecipeDto recipe,
        int fromServings,
        int toServings,
        CancellationToken cancellationToken);
}
