using Microsoft.EntityFrameworkCore;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Infrastructure.Persistence;

public sealed class ShoppingListRepository : IShoppingListRepository
{
    private readonly RecipesDbContext _dbContext;

    public ShoppingListRepository(RecipesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ShoppingList?> GetByIdAsync(ShoppingListId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShoppingLists
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(ShoppingList shoppingList, CancellationToken cancellationToken = default)
    {
        await _dbContext.ShoppingLists.AddAsync(shoppingList, cancellationToken);
    }

    public async Task<IReadOnlyList<ShoppingList>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShoppingLists
            .Include(x => x.Items)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShoppingList>> GetByHouseholdIdsAsync(
        IReadOnlyList<HouseholdId> householdIds,
        CancellationToken cancellationToken = default)
    {
        // EF Core can't translate any operation against a nullable strongly-typed
        // ID with a value conversion — filter client-side. Volumes are small.
        var ids = householdIds.Select(h => h.Value).ToHashSet();
        var all = await _dbContext.ShoppingLists
            .Include(x => x.Items)
            .ToListAsync(cancellationToken);
        return all
            .Where(x => x.HouseholdId.HasValue && ids.Contains(x.HouseholdId.Value.Value))
            .OrderBy(x => x.Name)
            .ToList();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}