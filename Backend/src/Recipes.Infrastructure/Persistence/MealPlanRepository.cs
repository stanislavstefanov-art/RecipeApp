using Microsoft.EntityFrameworkCore;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Infrastructure.Persistence;

public sealed class MealPlanRepository : IMealPlanRepository
{
    private readonly RecipesDbContext _dbContext;

    public MealPlanRepository(RecipesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MealPlan?> GetByIdAsync(MealPlanId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MealPlans
            .Include(x => x.Entries)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(MealPlan mealPlan, CancellationToken cancellationToken = default)
    {
        await _dbContext.MealPlans.AddAsync(mealPlan, cancellationToken);
    }

    public async Task<IReadOnlyList<MealPlan>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.MealPlans
            .Include(x => x.Entries)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MealPlan>> GetByHouseholdIdsAsync(
        IReadOnlyList<HouseholdId> householdIds,
        CancellationToken cancellationToken = default)
    {
        var ids = householdIds.Select(h => h.Value).ToHashSet();
        var all = await _dbContext.MealPlans
            .Include(x => x.Entries)
            .ToListAsync(cancellationToken);
        return all
            .Where(x => ids.Contains(x.HouseholdId.Value))
            .OrderBy(x => x.Name)
            .ToList();
    }

    public void Remove(MealPlan mealPlan) => _dbContext.MealPlans.Remove(mealPlan);

    public void RemoveRange(IEnumerable<MealPlan> mealPlans) => _dbContext.MealPlans.RemoveRange(mealPlans);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}