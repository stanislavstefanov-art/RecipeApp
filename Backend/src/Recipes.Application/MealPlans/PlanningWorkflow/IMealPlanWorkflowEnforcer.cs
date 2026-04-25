using Recipes.Application.MealPlans.SuggestMealPlan;

namespace Recipes.Application.MealPlans.PlanningWorkflow;

public interface IMealPlanWorkflowEnforcer
{
    Task<WorkflowResult> RunAsync(
        RunPlanningWorkflowCommand command,
        HouseholdPlanningProfileDto household,
        CancellationToken ct);
}
