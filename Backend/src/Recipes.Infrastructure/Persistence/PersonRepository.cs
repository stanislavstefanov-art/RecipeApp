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

    public Task AddAsync(Person person, CancellationToken cancellationToken = default)
        => _dbContext.Persons.AddAsync(person, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}