using Recipes.Domain.Primitives;

namespace Recipes.Application.Common;

public interface ICurrentUser
{
    UserId UserId { get; }
    Task<IReadOnlyList<HouseholdId>> GetHouseholdIdsAsync(CancellationToken ct);
    void InvalidateHouseholdCache();
}
