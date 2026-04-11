namespace Recipes.Application.MealPlans.SuggestMealPlan;

public interface IClaudeMealPlanSuggestionClient
{
    Task<MealPlanSuggestionDto> SuggestAsync(
        MealPlanSuggestionRequestDto request,
        string promptTemplate,
        string jsonSchema,
        CancellationToken cancellationToken);
}