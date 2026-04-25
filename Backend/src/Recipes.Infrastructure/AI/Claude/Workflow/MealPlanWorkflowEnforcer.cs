using Recipes.Application.MealPlans.PlanningWorkflow;
using Recipes.Application.MealPlans.SuggestMealPlan;
using Recipes.Application.MealPlans.SuggestMealPlanMultiAgent;

namespace Recipes.Infrastructure.AI.Claude.Workflow;

public sealed class MealPlanWorkflowEnforcer : IMealPlanWorkflowEnforcer
{
    private readonly IMealPlanOrchestratorAgent _orchestrator;
    private readonly IEnumerable<IWorkflowGate> _gates;

    public MealPlanWorkflowEnforcer(
        IMealPlanOrchestratorAgent orchestrator,
        IEnumerable<IWorkflowGate> gates)
    {
        _orchestrator = orchestrator;
        _gates        = gates;
    }

    public async Task<WorkflowResult> RunAsync(
        RunPlanningWorkflowCommand command,
        HouseholdPlanningProfileDto household,
        CancellationToken ct)
    {
        var multiAgentCommand = new SuggestMealPlanMultiAgentCommand(
            command.Name,
            command.HouseholdId,
            command.StartDate,
            command.NumberOfDays,
            command.MealTypes);

        var result = await _orchestrator.RunAsync(multiAgentCommand, household, ct);

        if (result.IsError)
            return new WorkflowResult(
                "failed",
                "orchestration",
                Draft:        null,
                GateResults:  [],
                BlockedReason: result.FirstError.Description);

        var draft      = result.Value;
        var ctx        = new WorkflowGateContext(draft, command.NumberOfDays, command.MealTypes, household.Members.Count);
        var gateResults = new List<GateCheckResult>();

        foreach (var gate in _gates)
        {
            var gr = gate.Check(ctx);
            gateResults.Add(gr);
            if (!gr.Passed)
                return new WorkflowResult("blocked", gate.Stage, draft, gateResults, gr.Message);
        }

        return new WorkflowResult("pending_approval", "human_approval", draft, gateResults);
    }
}
