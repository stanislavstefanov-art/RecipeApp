namespace Recipes.Domain.Repositories;

using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;

public interface IMealPlanRepository
{
    Task<MealPlan?> GetByIdAsync(MealPlanId id, CancellationToken cancellationToken = default);
    Task AddAsync(MealPlan mealPlan, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MealPlan>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}