using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;

namespace Recipes.Domain.Repositories;

public interface IMeasurementUnitRepository
{
    Task<IReadOnlyList<MeasurementUnit>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<MeasurementUnit?> GetByIdAsync(MeasurementUnitId id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByAbbreviationAsync(string abbreviation, CancellationToken cancellationToken = default);
    Task<int> GetNextSortOrderAsync(CancellationToken cancellationToken = default);
    Task AddAsync(MeasurementUnit unit, CancellationToken cancellationToken = default);
    void Remove(MeasurementUnit unit);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
