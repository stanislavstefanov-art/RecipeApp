using ErrorOr;
using MediatR;

namespace Recipes.Application.MealPlans.AddRecipeToMealPlan;

public sealed record AddRecipeToMealPlanCommand(
    Guid MealPlanId,
    Guid RecipeId,
    DateOnly PlannedDate,
    int MealType,
    int Scope,
    IReadOnlyList<MealPlanPersonAssignmentInputDto> Assignments) : IRequest<ErrorOr<Success>>;

public sealed record MealPlanPersonAssignmentInputDto(
    Guid PersonId,
    Guid AssignedRecipeId,
    Guid? RecipeVariationId,
    decimal PortionMultiplier,
    string? Notes);