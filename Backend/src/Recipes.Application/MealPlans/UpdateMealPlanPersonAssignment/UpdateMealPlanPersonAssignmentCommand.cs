using ErrorOr;
using MediatR;

namespace Recipes.Application.MealPlans.UpdateMealPlanPersonAssignment;

public sealed record UpdateMealPlanPersonAssignmentCommand(
    Guid MealPlanId,
    Guid MealPlanEntryId,
    Guid PersonId,
    Guid AssignedRecipeId,
    Guid? RecipeVariationId,
    decimal PortionMultiplier,
    string? Notes) : IRequest<ErrorOr<Success>>;