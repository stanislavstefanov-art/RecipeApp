using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Repositories;

namespace Recipes.Application.CookingLog.GetRecipeCookingStats;

public sealed class GetRecipeCookingStatsHandler
    : IRequestHandler<GetRecipeCookingStatsQuery, ErrorOr<IReadOnlyList<RecipeCookingStatDto>>>
{
    private readonly ICookingLogRepository _cookingLog;
    private readonly IRecipeRepository _recipes;
    private readonly ICurrentUser _currentUser;

    public GetRecipeCookingStatsHandler(ICookingLogRepository cookingLog, IRecipeRepository recipes, ICurrentUser currentUser)
    {
        _cookingLog = cookingLog;
        _recipes = recipes;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<IReadOnlyList<RecipeCookingStatDto>>> Handle(
        GetRecipeCookingStatsQuery request, CancellationToken cancellationToken)
    {
        var entries = await _cookingLog.GetAllByUserAsync(_currentUser.UserId, cancellationToken);

        var householdIds = await _currentUser.GetHouseholdIdsAsync(cancellationToken);
        var recipes = await _recipes.GetByHouseholdIdsAsync(householdIds, cancellationToken);
        var recipeNames = recipes.ToDictionary(r => r.Id, r => r.Name.Value);

        var stats = entries
            .GroupBy(e => e.RecipeId)
            .Select(g => new
            {
                RecipeId = g.Key,
                TimesCooked = g.Count(),
                LastCookedOn = g.Max(e => e.CookedOn),
            })
            .Where(s => recipeNames.ContainsKey(s.RecipeId))
            .OrderByDescending(s => s.TimesCooked)
            .ThenByDescending(s => s.LastCookedOn)
            .Select(s => new RecipeCookingStatDto(
                s.RecipeId.Value,
                recipeNames[s.RecipeId],
                s.TimesCooked,
                s.LastCookedOn))
            .ToList();

        return stats.ToErrorOr<IReadOnlyList<RecipeCookingStatDto>>();
    }
}
