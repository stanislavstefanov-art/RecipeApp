using Recipes.Application.MealPlans.SuggestMealPlan;

namespace Recipes.Application.MealPlans.PlanningWorkflow;

public interface IWorkflowGate
{
    string Name  { get; }
    string Stage { get; }
    GateCheckResult Check(WorkflowGateContext ctx);
}

public sealed record WorkflowGateContext(
    MealPlanSuggestionDto Draft,
    int ExpectedDays,
    IReadOnlyList<int> MealTypes,
    int MemberCount);
