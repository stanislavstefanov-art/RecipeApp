using ErrorOr;
using MediatR;

namespace Recipes.Application.MealPlans.RemoveMealPlanEntry;

public sealed record RemoveMealPlanEntryCommand(Guid MealPlanId, Guid EntryId) : IRequest<ErrorOr<Success>>;
