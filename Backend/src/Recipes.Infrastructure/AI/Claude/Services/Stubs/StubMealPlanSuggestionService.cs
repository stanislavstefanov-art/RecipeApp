using Recipes.Application.MealPlans.SuggestMealPlan;

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
                var recipe = request.AvailableRecipes[recipeIndex % request.AvailableRecipes.Count];

                entries.Add(new MealPlanSuggestionEntryDto(
                    recipe.RecipeId,
                    date,
                    mealType));

                recipeIndex++;
            }
        }

        var result = new MealPlanSuggestionDto(
            request.Name,
            entries,
            0.55,
            true,
            "Stub suggestion result. Replace with Claude-backed planning later.");

        return Task.FromResult(result);
    }
}