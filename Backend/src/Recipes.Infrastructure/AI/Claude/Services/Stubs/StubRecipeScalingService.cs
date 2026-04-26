using Recipes.Application.Recipes.GetRecipe;
using Recipes.Application.Recipes.ScaleRecipe;

namespace Recipes.Infrastructure.AI.Claude.Services.Stubs;

public sealed class StubRecipeScalingService : IRecipeScalingService
{
    public Task<ScaledRecipeDto> ScaleAsync(
        RecipeDto recipe,
        int fromServings,
        int toServings,
        CancellationToken cancellationToken)
    {
        var factor = (decimal)toServings / fromServings;

        var scaledIngredients = recipe.Ingredients
            .Select(i => new ScaledIngredientDto(
                Name: i.Name,
                Quantity: Math.Round(i.Quantity * factor, 2, MidpointRounding.AwayFromZero),
                Unit: i.Unit))
            .ToList();

        var result = new ScaledRecipeDto(
            RecipeId: recipe.Id,
            Name: recipe.Name,
            FromServings: fromServings,
            ToServings: toServings,
            Ingredients: scaledIngredients,
            AttemptsTaken: 1,
            ProvenanceId: Guid.Empty);

        return Task.FromResult(result);
    }
}
