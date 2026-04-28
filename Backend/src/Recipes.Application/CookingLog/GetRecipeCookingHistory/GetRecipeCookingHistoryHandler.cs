using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.CookingLog.GetRecipeCookingHistory;

public sealed class GetRecipeCookingHistoryHandler
    : IRequestHandler<GetRecipeCookingHistoryQuery, ErrorOr<IReadOnlyList<CookingLogEntryDto>>>
{
    private readonly IRecipeRepository _recipes;
    private readonly ICookingLogRepository _cookingLog;
    private readonly ICurrentUser _currentUser;

    public GetRecipeCookingHistoryHandler(
        IRecipeRepository recipes,
        ICookingLogRepository cookingLog,
        ICurrentUser currentUser)
    {
        _recipes = recipes;
        _cookingLog = cookingLog;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<IReadOnlyList<CookingLogEntryDto>>> Handle(
        GetRecipeCookingHistoryQuery request, CancellationToken cancellationToken)
    {
        var recipeId = RecipeId.From(request.RecipeId);
        var recipe = await _recipes.GetByIdAsync(recipeId, cancellationToken);

        if (recipe is null)
            return Error.NotFound("Recipe.NotFound", $"Recipe '{request.RecipeId}' was not found.");

        if (recipe.HouseholdId.HasValue)
        {
            var memberIds = await _currentUser.GetHouseholdIdsAsync(cancellationToken);
            if (!memberIds.Contains(recipe.HouseholdId.Value))
                return Error.NotFound("Recipe.NotFound", $"Recipe '{request.RecipeId}' was not found.");
        }

        var entries = await _cookingLog.GetByRecipeAndUserAsync(
            recipeId, _currentUser.UserId, limit: 20, cancellationToken);

        IReadOnlyList<CookingLogEntryDto> result = entries
            .Select(e => new CookingLogEntryDto(
                e.Id.Value,
                e.RecipeId.Value,
                recipe.Name.Value,
                e.CookedOn,
                e.Servings,
                e.Notes,
                e.CreatedAt))
            .ToList();

        return result.ToErrorOr();
    }
}
