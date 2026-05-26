using Microsoft.EntityFrameworkCore;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Infrastructure.Persistence;

public sealed class RecipeRepository : IRecipeRepository
{
    private readonly RecipesDbContext _db;

    public RecipeRepository(RecipesDbContext db)
    {
        _db = db;
    }

    public async Task<Recipe?> GetByIdAsync(RecipeId id, CancellationToken cancellationToken = default)
        => await _db.Recipes
            .Include(r => r.Ingredients)
            .Include(r => r.Steps)
            .Include(r => r.Ratings)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Recipe>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _db.Recipes
            .AsNoTracking()
            .OrderBy(r => EF.Property<string>(r, "Name"))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Recipe>> GetByHouseholdIdsAsync(
        IReadOnlyList<HouseholdId> householdIds,
        CancellationToken cancellationToken = default)
    {
        // EF Core can't translate any comparison against a nullable strongly-typed
        // ID with a value conversion — not r.HouseholdId != null, not Contains.
        // Filter entirely client-side. Volumes are small on the free-tier app.
        var ids = householdIds.Select(h => h.Value).ToHashSet();
        var all = await _db.Recipes
            .AsNoTracking()
            .Include(r => r.Ingredients)
            .Include(r => r.Ratings)
            .ToListAsync(cancellationToken);
        return all
            .Where(r => r.HouseholdId.HasValue && ids.Contains(r.HouseholdId.Value.Value))
            .OrderBy(r => r.Name.Value)
            .ToList();
    }

    public async Task<IReadOnlyList<Recipe>> SearchByIngredientNameAsync(
        string ingredientName, CancellationToken cancellationToken = default)
        => await _db.Recipes
            .AsNoTracking()
            .Where(r => r.Ingredients.Any(i => EF.Functions.Like(i.Name, $"%{ingredientName}%")))
            .OrderBy(r => EF.Property<string>(r, "Name"))
            .ToListAsync(cancellationToken);

    public void Add(Recipe recipe) => _db.Recipes.Add(recipe);

    public void Remove(Recipe recipe) => _db.Recipes.Remove(recipe);

    public void RemoveRange(IEnumerable<Recipe> recipes) => _db.Recipes.RemoveRange(recipes);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _db.SaveChangesAsync(cancellationToken);
}
