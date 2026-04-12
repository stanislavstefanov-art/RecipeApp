namespace Recipes.Domain.Repositories;

using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;

public interface IHouseholdRepository
{
    Task<Household?> GetByIdAsync(HouseholdId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Household>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Household household, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}