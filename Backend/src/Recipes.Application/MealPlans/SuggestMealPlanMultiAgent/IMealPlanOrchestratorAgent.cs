using ErrorOr;
using Recipes.Application.MealPlans.SuggestMealPlan;

namespace Recipes.Application.MealPlans.SuggestMealPlanMultiAgent;

public interface IMealPlanOrchestratorAgent
{
    Task<ErrorOr<MealPlanSuggestionDto>> RunAsync(
        SuggestMealPlanMultiAgentCommand command,
        HouseholdPlanningProfileDto household,
        CancellationToken ct);
}
