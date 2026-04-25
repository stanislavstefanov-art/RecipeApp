using Recipes.Application.Recipes.GetRecipe;

namespace Recipes.Application.Recipes.CritiqueRecipe;

public interface IRecipeCritiqueService
{
    Task<RecipeCritiqueDto> CritiqueAsync(RecipeDto recipe, CancellationToken cancellationToken);
}
