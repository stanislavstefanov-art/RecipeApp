using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;

namespace Recipes.Domain.Repositories;

public interface ICookingLogRepository
{
    void Add(CookingLogEntry entry);
    void Remove(CookingLogEntry entry);
    Task<CookingLogEntry?> GetByIdAsync(CookingLogEntryId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CookingLogEntry>> GetByRecipeAndUserAsync(
        RecipeId recipeId, UserId userId, int limit, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
