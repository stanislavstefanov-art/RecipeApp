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

        // Track how many times each recipe has been used; never exceed MealsPerCook.
        var usageCount = request.AvailableRecipes.ToDictionary(r => r.RecipeId, _ => 0);

        AvailableRecipeDto PickRecipe(int slotIndex)
        {
            // Try recipes in round-robin order, skipping any that have hit their MealsPerCook cap.
            for (var offset = 0; offset < request.AvailableRecipes.Count; offset++)
            {
                var candidate = request.AvailableRecipes[(slotIndex + offset) % request.AvailableRecipes.Count];
                if (usageCount[candidate.RecipeId] < candidate.MealsPerCook)
                    return candidate;
            }
            // All recipes exhausted — fall back to the first one to avoid returning null.
            return request.AvailableRecipes[slotIndex % request.AvailableRecipes.Count];
        }

        var slotIndex = 0;
        for (var day = 0; day < request.NumberOfDays; day++)
        {
            var date = request.StartDate.AddDays(day);

            foreach (var mealType in request.MealTypes)
            {
                var baseRecipe = PickRecipe(slotIndex);
                usageCount[baseRecipe.RecipeId]++;

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
                    null,
                    date,
                    mealType,
                    (int)scope,
                    assignments));

                slotIndex++;
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