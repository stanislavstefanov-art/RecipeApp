using ErrorOr;
using MediatR;
using Recipes.Application.MealPlans.SuggestMealPlan;

namespace Recipes.Application.MealPlans.SuggestMealPlanMultiAgent;

public sealed record SuggestMealPlanMultiAgentCommand(
    string Name,
    Guid HouseholdId,
    DateOnly StartDate,
    int NumberOfDays,
    IReadOnlyList<int> MealTypes) : IRequest<ErrorOr<MealPlanSuggestionDto>>;
