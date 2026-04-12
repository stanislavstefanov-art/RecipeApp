using ErrorOr;
using MediatR;

namespace Recipes.Application.MealPlans.AcceptMealPlanSuggestion;

public sealed record AcceptMealPlanSuggestionCommand(
    string Name,
    Guid HouseholdId,
    IReadOnlyList<AcceptMealPlanSuggestionEntryDto> Entries) : IRequest<ErrorOr<AcceptMealPlanSuggestionResponse>>;

public sealed record AcceptMealPlanSuggestionEntryDto(
    Guid BaseRecipeId,
    DateOnly PlannedDate,
    int MealType,
    int Scope,
    IReadOnlyList<AcceptMealPlanSuggestionAssignmentDto> Assignments);

public sealed record AcceptMealPlanSuggestionAssignmentDto(
    Guid PersonId,
    Guid AssignedRecipeId,
    Guid? RecipeVariationId,
    decimal PortionMultiplier,
    string? Notes);