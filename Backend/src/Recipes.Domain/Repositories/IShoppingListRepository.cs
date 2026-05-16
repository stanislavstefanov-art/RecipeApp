namespace Recipes.Domain.Repositories;

using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;

public interface IShoppingListRepository
{
    Task<ShoppingList?> GetByIdAsync(ShoppingListId id, CancellationToken cancellationToken = default);
    Task AddAsync(ShoppingList shoppingList, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShoppingList>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShoppingList>> GetByHouseholdIdsAsync(IReadOnlyList<HouseholdId> householdIds, CancellationToken cancellationToken = default);
    void Remove(ShoppingList shoppingList);
    void RemoveRange(IEnumerable<ShoppingList> shoppingLists);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}