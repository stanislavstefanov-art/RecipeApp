namespace Recipes.Application.MealPlans.SuggestMealPlan;

public sealed record MealPlanSuggestionDto(
    string Name,
    IReadOnlyList<MealPlanSuggestionEntryDto> Entries,
    double Confidence,
    bool NeedsReview,
    string? Notes);

public sealed record MealPlanSuggestionEntryDto(
    Guid RecipeId,
    DateOnly PlannedDate,
    int MealType);