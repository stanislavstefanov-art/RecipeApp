namespace Recipes.Application.MealPlans.GetMealPlan;

public sealed record MealPlanDetailsDto(
    Guid Id,
    string Name,
    IReadOnlyList<MealPlanEntryDto> Entries);

public sealed record MealPlanEntryDto(
    Guid Id,
    Guid RecipeId,
    DateOnly PlannedDate,
    int MealType);