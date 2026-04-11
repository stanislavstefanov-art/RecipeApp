using ErrorOr;
using MediatR;

namespace Recipes.Application.MealPlans.GenerateShoppingListFromMealPlan;

public sealed record GenerateShoppingListFromMealPlanCommand(
    Guid MealPlanId,
    Guid ShoppingListId) : IRequest<ErrorOr<Success>>;