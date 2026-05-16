namespace Recipes.Domain.Repositories;

using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;

public interface IMealPlanRepository
{
    Task<MealPlan?> GetByIdAsync(MealPlanId id, CancellationToken cancellationToken = default);
    Task AddAsync(MealPlan mealPlan, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MealPlan>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MealPlan>> GetByHouseholdIdsAsync(IReadOnlyList<HouseholdId> householdIds, CancellationToken cancellationToken = default);
    void Remove(MealPlan mealPlan);
    void RemoveRange(IEnumerable<MealPlan> mealPlans);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}