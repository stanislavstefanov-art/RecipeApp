using Microsoft.EntityFrameworkCore;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Infrastructure.Persistence;

public sealed class MeasurementUnitRepository : IMeasurementUnitRepository
{
    private readonly RecipesDbContext _dbContext;

    public MeasurementUnitRepository(RecipesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<MeasurementUnit>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.MeasurementUnits
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<MeasurementUnit?> GetByIdAsync(MeasurementUnitId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MeasurementUnits
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByAbbreviationAsync(string abbreviation, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MeasurementUnits
            .AnyAsync(x => x.Abbreviation == abbreviation, cancellationToken);
    }

    public async Task<int> GetNextSortOrderAsync(CancellationToken cancellationToken = default)
    {
        var max = await _dbContext.MeasurementUnits
            .MaxAsync(x => (int?)x.SortOrder, cancellationToken);
        return (max ?? 0) + 1;
    }

    public async Task AddAsync(MeasurementUnit unit, CancellationToken cancellationToken = default)
    {
        await _dbContext.MeasurementUnits.AddAsync(unit, cancellationToken);
    }

    public void Remove(MeasurementUnit unit)
    {
        _dbContext.MeasurementUnits.Remove(unit);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
