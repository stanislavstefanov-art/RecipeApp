using ErrorOr;
using MediatR;

namespace Recipes.Application.MealPlans.SwapMealPlanEntries;

public sealed record SwapMealPlanEntriesCommand(Guid MealPlanId, Guid EntryAId, Guid EntryBId) : IRequest<ErrorOr<Success>>;
