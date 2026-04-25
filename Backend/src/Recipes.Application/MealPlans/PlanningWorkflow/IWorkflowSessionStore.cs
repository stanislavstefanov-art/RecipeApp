namespace Recipes.Application.MealPlans.PlanningWorkflow;

public interface IWorkflowSessionStore
{
    Guid Save(WorkflowResult result, int numberOfDays, IReadOnlyList<int> mealTypes);
    WorkflowSession? Get(Guid sessionId);
}
