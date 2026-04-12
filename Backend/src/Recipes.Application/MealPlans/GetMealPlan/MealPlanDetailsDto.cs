namespace Recipes.Application.MealPlans.GetMealPlan;

public sealed record MealPlanDetailsDto(
    Guid Id,
    string Name,
    Guid HouseholdId,
    string HouseholdName,
    IReadOnlyList<MealPlanEntryDto> Entries);

public sealed record MealPlanEntryDto(
    Guid Id,
    Guid BaseRecipeId,
    string BaseRecipeName,
    DateOnly PlannedDate,
    int MealType,
    int Scope,
    IReadOnlyList<MealPlanEntryAssignmentDto> Assignments);

public sealed record MealPlanEntryAssignmentDto(
    Guid PersonId,
    string PersonName,
    Guid AssignedRecipeId,
    string AssignedRecipeName,
    Guid? RecipeVariationId,
    string? RecipeVariationName,
    decimal PortionMultiplier,
    string? Notes);