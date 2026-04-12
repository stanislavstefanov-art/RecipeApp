using ErrorOr;
using MediatR;

namespace Recipes.Application.MealPlans.RegenerateShoppingListFromMealPlan;

public sealed record RegenerateShoppingListFromMealPlanCommand(
    Guid MealPlanId,
    Guid ShoppingListId) : IRequest<ErrorOr<Success>>;