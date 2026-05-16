namespace Recipes.Domain.Repositories;

using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;

public interface IPersonRepository
{
    Task<Person?> GetByIdAsync(PersonId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Person>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Person>> GetByIdsAsync(IEnumerable<PersonId> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Person>> GetByHouseholdIdsAsync(IReadOnlyList<HouseholdId> householdIds, CancellationToken cancellationToken = default);
    Task AddAsync(Person person, CancellationToken cancellationToken = default);
    void Remove(Person person);
    void RemoveRange(IEnumerable<Person> persons);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}