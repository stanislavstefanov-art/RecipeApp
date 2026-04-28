using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Recipes.Application.Abstractions;
using Recipes.Application.Common;
using Recipes.Domain.Primitives;

namespace Recipes.Api.Auth;

public sealed class CurrentUser : ICurrentUser
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMemoryCache _cache;
    private readonly IRecipesDbContext _db;

    public CurrentUser(
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache cache,
        IRecipesDbContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _cache = cache;
        _db = db;
    }

    public UserId UserId
    {
        get
        {
            var sub = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");
            if (sub is null || !Guid.TryParse(sub, out var guid))
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            return UserId.From(guid);
        }
    }

    public async Task<IReadOnlyList<HouseholdId>> GetHouseholdIdsAsync(CancellationToken ct)
    {
        var userId = UserId;
        var cacheKey = $"households:{userId.Value}";

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<HouseholdId>? cached) && cached is not null)
        {
            return cached;
        }

        var ids = await _db.HouseholdMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.HouseholdId)
            .ToListAsync(ct);

        var result = ids.Cast<HouseholdId>().ToList().AsReadOnly();
        _cache.Set(cacheKey, (IReadOnlyList<HouseholdId>)result, CacheTtl);
        return result;
    }

    public void InvalidateHouseholdCache()
    {
        var userId = UserId;
        _cache.Remove($"households:{userId.Value}");
    }
}
