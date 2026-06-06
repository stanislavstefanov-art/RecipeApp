using ErrorOr;
using MediatR;

namespace Recipes.Application.MealPlans.SuggestMealPlan;

public sealed record SuggestMealPlanCommand(
    string Name,
    Guid HouseholdId,
    DateOnly StartDate,
    int NumberOfDays,
    IReadOnlyList<int> MealTypes,
    string RecipeSource = "all",
    string RecipeOrigin = "all") : IRequest<ErrorOr<MealPlanSuggestionDto>>;