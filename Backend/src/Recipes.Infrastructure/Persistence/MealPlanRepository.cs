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

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}