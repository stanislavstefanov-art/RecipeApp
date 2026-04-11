using ErrorOr;
using MediatR;

namespace Recipes.Application.MealPlans.AcceptMealPlanSuggestion;

public sealed record AcceptMealPlanSuggestionCommand(
    string Name,
    IReadOnlyList<AcceptMealPlanSuggestionEntryDto> Entries) : IRequest<ErrorOr<AcceptMealPlanSuggestionResponse>>;