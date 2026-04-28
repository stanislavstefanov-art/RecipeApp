using Microsoft.EntityFrameworkCore;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Infrastructure.Persistence;

public sealed class CookingLogRepository : ICookingLogRepository
{
    private readonly RecipesDbContext _db;

    public CookingLogRepository(RecipesDbContext db)
    {
        _db = db;
    }

    public void Add(CookingLogEntry entry) => _db.CookingLogEntries.Add(entry);

    public void Remove(CookingLogEntry entry) => _db.CookingLogEntries.Remove(entry);

    public async Task<CookingLogEntry?> GetByIdAsync(
        CookingLogEntryId id, CancellationToken cancellationToken = default)
        => await _db.CookingLogEntries
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CookingLogEntry>> GetByRecipeAndUserAsync(
        RecipeId recipeId, UserId userId, int limit, CancellationToken cancellationToken = default)
        => await _db.CookingLogEntries
            .Where(e => e.RecipeId == recipeId && e.UserId == userId)
            .OrderByDescending(e => e.CookedOn)
            .ThenByDescending(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _db.SaveChangesAsync(cancellationToken);
}
