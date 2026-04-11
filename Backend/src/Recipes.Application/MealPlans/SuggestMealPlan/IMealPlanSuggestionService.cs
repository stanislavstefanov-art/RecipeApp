namespace Recipes.Application.MealPlans.SuggestMealPlan;

public interface IMealPlanSuggestionService
{
    Task<MealPlanSuggestionDto> SuggestAsync(
        MealPlanSuggestionRequestDto request,
        CancellationToken cancellationToken);
}