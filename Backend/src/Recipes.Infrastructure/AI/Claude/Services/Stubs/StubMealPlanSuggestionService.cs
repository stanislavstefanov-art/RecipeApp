using Recipes.Application.MealPlans.SuggestMealPlan;
using Recipes.Domain.Enums;

namespace Recipes.Infrastructure.AI.Claude.Services.Stubs;

public sealed class StubMealPlanSuggestionService : IMealPlanSuggestionService
{
    public Task<MealPlanSuggestionDto> SuggestAsync(
        MealPlanSuggestionRequestDto request,
        CancellationToken cancellationToken)
    {
        var entries = new List<MealPlanSuggestionEntryDto>();
        var recipeIndex = 0;

        for (var day = 0; day < request.NumberOfDays; day++)
        {
            var date = request.StartDate.AddDays(day);

            foreach (var mealType in request.MealTypes)
            {
                var baseRecipe = request.AvailableRecipes[recipeIndex % request.AvailableRecipes.Count];

                var assignments = request.Household.Members
                    .Select(member =>
                    {
                        var highProtein = member.Notes?.Contains("protein", StringComparison.OrdinalIgnoreCase) == true;
                        var vegetarianLike = member.Notes?.Contains("vegetarian", StringComparison.OrdinalIgnoreCase) == true
                                             || member.Notes?.Contains("fish", StringComparison.OrdinalIgnoreCase) == true;

                        var selectedVariation = vegetarianLike
                            ? baseRecipe.Variations.FirstOrDefault(v =>
                                v.Name.Contains("vegetarian", StringComparison.OrdinalIgnoreCase) ||
                                v.Name.Contains("pesc", StringComparison.OrdinalIgnoreCase))
                            : highProtein
                                ? baseRecipe.Variations.FirstOrDefault(v =>
                                    v.Name.Contains("protein", StringComparison.OrdinalIgnoreCase))
                                : null;

                        var portion = highProtein ? 1.5m : 1.0m;

                        return new MealPlanSuggestionAssignmentDto(
                            member.PersonId,
                            baseRecipe.RecipeId,
                            selectedVariation?.RecipeVariationId,
                            portion,
                            selectedVariation?.Notes ?? member.Notes);
                    })
                    .ToList();

                var scope = assignments.Any(a => a.RecipeVariationId.HasValue)
                    ? MealScope.SharedWithVariations
                    : MealScope.Shared;

                entries.Add(new MealPlanSuggestionEntryDto(
                    baseRecipe.RecipeId,
                    date,
                    mealType,
                    (int)scope,
                    assignments));

                recipeIndex++;
            }
        }

        var result = new MealPlanSuggestionDto(
            request.Name,
            entries,
            0.65,
            true,
            "Stub variation-aware suggestion. Replace with Claude-backed planning later.");

        return Task.FromResult(result);
    }
}