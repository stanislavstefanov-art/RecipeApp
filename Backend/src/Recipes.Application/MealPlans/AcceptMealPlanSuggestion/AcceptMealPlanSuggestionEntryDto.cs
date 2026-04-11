namespace Recipes.Application.MealPlans.AcceptMealPlanSuggestion;

public sealed record AcceptMealPlanSuggestionEntryDto(
    Guid RecipeId,
    DateOnly PlannedDate,
    int MealType);