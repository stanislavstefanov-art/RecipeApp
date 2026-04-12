namespace Recipes.Application.MealPlans.SuggestMealPlan;

public sealed record MealPlanSuggestionRequestDto(
    string Name,
    DateOnly StartDate,
    int NumberOfDays,
    IReadOnlyList<int> MealTypes,
    HouseholdPlanningProfileDto Household,
    IReadOnlyList<AvailableRecipeDto> AvailableRecipes);

public sealed record AvailableRecipeDto(
    Guid RecipeId,
    string Name,
    IReadOnlyList<AvailableRecipeVariationDto> Variations);

public sealed record AvailableRecipeVariationDto(
    Guid RecipeVariationId,
    string Name,
    string? Notes,
    string? IngredientAdjustmentNotes);

public sealed record HouseholdPlanningProfileDto(
    Guid HouseholdId,
    string HouseholdName,
    IReadOnlyList<PersonPlanningProfileDto> Members);

public sealed record PersonPlanningProfileDto(
    Guid PersonId,
    string Name,
    IReadOnlyList<int> DietaryPreferences,
    IReadOnlyList<int> HealthConcerns,
    string? Notes);