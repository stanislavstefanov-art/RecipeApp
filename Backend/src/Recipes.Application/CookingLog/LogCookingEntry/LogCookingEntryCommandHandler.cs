using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.CookingLog.LogCookingEntry;

public sealed class LogCookingEntryCommandHandler : IRequestHandler<LogCookingEntryCommand, ErrorOr<CookingLogEntryDto>>
{
    private readonly IRecipeRepository _recipes;
    private readonly ICookingLogRepository _cookingLog;
    private readonly ICurrentUser _currentUser;

    public LogCookingEntryCommandHandler(
        IRecipeRepository recipes,
        ICookingLogRepository cookingLog,
        ICurrentUser currentUser)
    {
        _recipes = recipes;
        _cookingLog = cookingLog;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<CookingLogEntryDto>> Handle(
        LogCookingEntryCommand request, CancellationToken cancellationToken)
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

        var entry = new CookingLogEntry(
            recipeId,
            _currentUser.UserId,
            recipe.HouseholdId,
            request.CookedOn,
            request.Servings,
            request.Notes,
            DateTimeOffset.UtcNow);

        _cookingLog.Add(entry);
        await _cookingLog.SaveChangesAsync(cancellationToken);

        return new CookingLogEntryDto(
            entry.Id.Value,
            entry.RecipeId.Value,
            recipe.Name.Value,
            entry.CookedOn,
            entry.Servings,
            entry.Notes,
            entry.CreatedAt);
    }
}
