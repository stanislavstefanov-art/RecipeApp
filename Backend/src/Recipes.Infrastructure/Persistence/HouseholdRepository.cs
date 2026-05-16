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
            .Include(x => x.People)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Household>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbContext.Households
            .Include(x => x.Members)
            .Include(x => x.People)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Household>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
        => await _dbContext.Households
            .Include(x => x.Members)
            .Include(x => x.People)
            .Where(h => h.Members.Any(m => m.UserId == userId))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task AddAsync(Household household, CancellationToken cancellationToken = default)
        => _dbContext.Households.AddAsync(household, cancellationToken).AsTask();

    public void Remove(Household household) => _dbContext.Households.Remove(household);

    public void RemoveRange(IEnumerable<Household> households) => _dbContext.Households.RemoveRange(households);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}