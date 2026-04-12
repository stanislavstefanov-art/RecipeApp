using ErrorOr;
using MediatR;

namespace Recipes.Application.MealPlans.CreateMealPlan;

public sealed record CreateMealPlanCommand(string Name, Guid HouseholdId)
    : IRequest<ErrorOr<CreateMealPlanResponse>>;