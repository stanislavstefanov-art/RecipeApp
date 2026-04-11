using ErrorOr;
using MediatR;

namespace Recipes.Application.MealPlans.SuggestMealPlan;

public sealed record SuggestMealPlanCommand(
    string Name,
    DateOnly StartDate,
    int NumberOfDays,
    IReadOnlyList<int> MealTypes) : IRequest<ErrorOr<MealPlanSuggestionDto>>;