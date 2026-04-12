using ErrorOr;
using MediatR;

namespace Recipes.Application.MealPlans.SuggestMealPlan;

public sealed record SuggestMealPlanCommand(
    string Name,
    Guid HouseholdId,
    DateOnly StartDate,
    int NumberOfDays,
    IReadOnlyList<int> MealTypes) : IRequest<ErrorOr<MealPlanSuggestionDto>>;