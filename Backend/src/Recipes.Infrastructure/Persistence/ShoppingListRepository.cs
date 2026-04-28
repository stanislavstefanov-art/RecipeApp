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
        return await _dbContext.ShoppingLists
            .Include(x => x.Items)
            .Where(x => x.HouseholdId != null && householdIds.Contains(x.HouseholdId.Value))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}