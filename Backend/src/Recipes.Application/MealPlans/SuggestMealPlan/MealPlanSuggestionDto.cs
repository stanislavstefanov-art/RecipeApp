namespace Recipes.Application.MealPlans.SuggestMealPlan;

public sealed record MealPlanSuggestionDto(
    string Name,
    IReadOnlyList<MealPlanSuggestionEntryDto> Entries,
    double Confidence,
    bool NeedsReview,
    string? Notes);

public sealed record MealPlanSuggestionEntryDto(
    Guid BaseRecipeId,
    DateOnly PlannedDate,
    int MealType,
    int Scope,
    IReadOnlyList<MealPlanSuggestionAssignmentDto> Assignments);

public sealed record MealPlanSuggestionAssignmentDto(
    Guid PersonId,
    Guid AssignedRecipeId,
    Guid? RecipeVariationId,
    decimal PortionMultiplier,
    string? Notes);