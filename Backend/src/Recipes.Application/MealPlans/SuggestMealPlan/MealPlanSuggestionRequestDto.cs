namespace Recipes.Application.MealPlans.SuggestMealPlan;

public sealed record MealPlanSuggestionRequestDto(
    string Name,
    DateOnly StartDate,
    int NumberOfDays,
    IReadOnlyList<int> MealTypes,
    IReadOnlyList<AvailableRecipeDto> AvailableRecipes);

public sealed record AvailableRecipeDto(
    Guid RecipeId,
    string Name);