using Microsoft.EntityFrameworkCore;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Infrastructure.Persistence;

public sealed class HouseholdRepository : IHouseholdRepository
{
    private readonly RecipesDbContext _dbContext;

    public HouseholdRepository(RecipesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Household?> GetByIdAsync(HouseholdId id, CancellationToken cancellationToken = default)
        => _dbContext.Households
            .Include(x => x.Members)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Household>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbContext.Households
            .Include(x => x.Members)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task AddAsync(Household household, CancellationToken cancellationToken = default)
        => _dbContext.Households.AddAsync(household, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}