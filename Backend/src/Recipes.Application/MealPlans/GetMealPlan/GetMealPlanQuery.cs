using ErrorOr;
using MediatR;

namespace Recipes.Application.MealPlans.GetMealPlan;

public sealed record GetMealPlanQuery(Guid MealPlanId) : IRequest<ErrorOr<MealPlanDetailsDto>>;