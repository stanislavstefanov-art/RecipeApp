using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;

namespace Recipes.Domain.Repositories;

public interface IRecipeRepository
{
    /// <summary>Loads Recipe with Ingredients and Steps included.</summary>
    Task<Recipe?> GetByIdAsync(RecipeId id, CancellationToken cancellationToken = default);

    /// <summary>Loads all recipes without child collections (list view only needs Id + Name).</summary>
    Task<IReadOnlyList<Recipe>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Recipe>> SearchByIngredientNameAsync(
        string ingredientName, CancellationToken cancellationToken = default);

    void Add(Recipe recipe);
    void Remove(Recipe recipe);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
