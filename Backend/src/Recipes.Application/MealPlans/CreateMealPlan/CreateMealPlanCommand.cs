using ErrorOr;
using MediatR;

namespace Recipes.Application.MealPlans.CreateMealPlan;

public sealed record CreateMealPlanCommand(string Name) : IRequest<ErrorOr<CreateMealPlanResponse>>;