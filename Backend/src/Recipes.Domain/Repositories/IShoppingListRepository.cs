namespace Recipes.Domain.Repositories;

using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;

public interface IShoppingListRepository
{
    Task<ShoppingList?> GetByIdAsync(ShoppingListId id, CancellationToken cancellationToken = default);
    Task AddAsync(ShoppingList shoppingList, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShoppingList>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}