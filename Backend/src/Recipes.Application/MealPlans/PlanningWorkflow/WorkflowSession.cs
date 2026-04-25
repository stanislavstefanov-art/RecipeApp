namespace Recipes.Application.MealPlans.PlanningWorkflow;

public sealed record WorkflowSession(
    Guid SessionId,
    WorkflowResult WorkflowResult,
    int NumberOfDays,
    IReadOnlyList<int> MealTypes,
    DateTime CreatedAt,
    DateTime ExpiresAt);

public sealed record WorkflowSessionResult(
    Guid SessionId,
    WorkflowResult WorkflowResult,
    bool IsEscalated = false);
