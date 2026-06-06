using Microsoft.EntityFrameworkCore;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Infrastructure.Persistence;

public sealed class PantryRepository : IPantryRepository
{
    private readonly RecipesDbContext _db;
    public PantryRepository(RecipesDbContext db) => _db = db;

    public async Task AddAsync(PantryItem item, CancellationToken cancellationToken = default)
        => await _db.PantryItems.AddAsync(item, cancellationToken);

    public void Remove(PantryItem item) => _db.PantryItems.Remove(item);

    public async Task<PantryItem?> GetByIdAsync(PantryItemId id, CancellationToken cancellationToken = default)
        => await _db.PantryItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PantryItem>> GetByUserAsync(UserId userId, CancellationToken cancellationToken = default)
        => await _db.PantryItems
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.IngredientName)
            .ToListAsync(cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _db.SaveChangesAsync(cancellationToken);
}
