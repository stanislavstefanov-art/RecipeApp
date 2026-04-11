using ErrorOr;
using MediatR;

namespace Recipes.Application.MealPlans.AddRecipeToMealPlan;

public sealed record AddRecipeToMealPlanCommand(
    Guid MealPlanId,
    Guid RecipeId,
    DateOnly PlannedDate,
    int MealType) : IRequest<ErrorOr<Success>>;