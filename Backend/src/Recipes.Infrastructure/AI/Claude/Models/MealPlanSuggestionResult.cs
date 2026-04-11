namespace Recipes.Infrastructure.AI.Claude.Models;

public sealed record MealPlanSuggestionResult(
    string Name,
    IReadOnlyList<MealPlanSuggestionResultEntry> Entries,
    double Confidence,
    bool NeedsReview,
    string? Notes);

public sealed record MealPlanSuggestionResultEntry(
    Guid RecipeId,
    DateOnly PlannedDate,
    int MealType);