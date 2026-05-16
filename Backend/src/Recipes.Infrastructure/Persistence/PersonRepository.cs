using Microsoft.EntityFrameworkCore;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Infrastructure.Persistence;

public sealed class PersonRepository : IPersonRepository
{
    private readonly RecipesDbContext _dbContext;

    public PersonRepository(RecipesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Person?> GetByIdAsync(PersonId id, CancellationToken cancellationToken = default)
        => _dbContext.Persons.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Person>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbContext.Persons.OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Person>> GetByIdsAsync(IEnumerable<PersonId> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await _dbContext.Persons
            .Where(x => idList.Contains(x.Id))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Person>> GetByHouseholdIdsAsync(
        IReadOnlyList<HouseholdId> householdIds,
        CancellationToken cancellationToken = default)
    {
        // EF Core can't translate any operation against a nullable strongly-typed
        // ID with a value conversion — filter client-side. Volumes are small.
        var ids = householdIds.Select(h => h.Value).ToHashSet();
        var all = await _dbContext.Persons.ToListAsync(cancellationToken);
        return all
            .Where(x => x.HouseholdId.HasValue && ids.Contains(x.HouseholdId.Value.Value))
            .OrderBy(x => x.Name)
            .ToList();
    }

    public Task AddAsync(Person person, CancellationToken cancellationToken = default)
        => _dbContext.Persons.AddAsync(person, cancellationToken).AsTask();

    public void Remove(Person person) => _dbContext.Persons.Remove(person);

    public void RemoveRange(IEnumerable<Person> persons) => _dbContext.Persons.RemoveRange(persons);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}