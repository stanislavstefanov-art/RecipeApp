using ErrorOr;
using MediatR;

namespace Recipes.Application.MealPlans.DeleteMealPlan;

public sealed record DeleteMealPlanCommand(Guid Id) : IRequest<ErrorOr<Deleted>>;
