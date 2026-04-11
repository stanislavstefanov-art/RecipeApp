using ErrorOr;
using MediatR;

namespace Recipes.Application.MealPlans.ListMealPlans;

public sealed record ListMealPlansQuery() : IRequest<ErrorOr<IReadOnlyList<MealPlanListItemDto>>>;