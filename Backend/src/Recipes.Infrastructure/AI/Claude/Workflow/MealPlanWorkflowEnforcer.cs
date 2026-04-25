using System.Text.RegularExpressions;
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

        var confidence = ComputeConfidence(gateResults);
        return new WorkflowResult("pending_approval", "human_approval", draft, gateResults,
            Confidence: confidence);
    }

    private static string ComputeConfidence(IReadOnlyList<GateCheckResult> gateResults)
    {
        if (gateResults.Any(g =>
                g.Message.Contains("Only", StringComparison.OrdinalIgnoreCase) ||
                g.Message.Contains("warning", StringComparison.OrdinalIgnoreCase)))
            return "low";

        var slotGate = gateResults.FirstOrDefault(g => g.GateName == "MinSlotsCovered");
        if (slotGate is not null)
        {
            var match = Regex.Match(slotGate.Message, @"(\d+)/(\d+)");
            if (match.Success &&
                int.TryParse(match.Groups[1].Value, out var actual) &&
                int.TryParse(match.Groups[2].Value, out var expected) &&
                expected > 0 &&
                (double)actual / expected >= 1.20)
                return "high";
        }

        return "medium";
    }
}
