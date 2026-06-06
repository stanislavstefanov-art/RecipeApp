using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;

namespace Recipes.Domain.Repositories;

public interface IPantryRepository
{
    Task AddAsync(PantryItem item, CancellationToken cancellationToken = default);
    void Remove(PantryItem item);
    Task<PantryItem?> GetByIdAsync(PantryItemId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PantryItem>> GetByUserAsync(UserId userId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
