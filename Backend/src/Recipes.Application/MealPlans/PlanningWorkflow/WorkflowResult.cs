using Recipes.Application.MealPlans.SuggestMealPlan;

namespace Recipes.Application.MealPlans.PlanningWorkflow;

public sealed record WorkflowResult(
    string Status,
    string CurrentStage,
    MealPlanSuggestionDto? Draft,
    IReadOnlyList<GateCheckResult> GateResults,
    string? BlockedReason = null,
    string Confidence = "medium");

public sealed record GateCheckResult(string GateName, bool Passed, string Message);
