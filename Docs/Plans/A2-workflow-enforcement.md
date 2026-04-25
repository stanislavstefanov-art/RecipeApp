# A2 — Workflow Enforcement and Handoff: Implementation Plan

Reference spec: `Docs/specs/A2-workflow-enforcement.md`

Build order: DTOs → gate interface → gate implementations → enforcer interface →
enforcer implementation → commands + handlers → endpoints → DI → CCAF doc.

---

## Step 1 — WorkflowResult DTOs

Create `Backend/src/Recipes.Application/MealPlans/PlanningWorkflow/WorkflowResult.cs`:

```csharp
public sealed record WorkflowResult(
    string Status,         // "pending_approval" | "blocked" | "failed"
    string CurrentStage,
    MealPlanSuggestionDto? Draft,
    IReadOnlyList<GateCheckResult> GateResults,
    string? BlockedReason = null);

public sealed record GateCheckResult(string GateName, bool Passed, string Message);
```

---

## Step 2 — IWorkflowGate + WorkflowGateContext

Create `Backend/src/Recipes.Application/MealPlans/PlanningWorkflow/IWorkflowGate.cs`:

```csharp
public interface IWorkflowGate
{
    string Name { get; }
    string Stage { get; }
    GateCheckResult Check(WorkflowGateContext ctx);
}

public sealed record WorkflowGateContext(
    MealPlanSuggestionDto Draft,
    int ExpectedDays,
    IReadOnlyList<int> MealTypes,
    int MemberCount);
```

---

## Step 3 — Gate implementations

Create `Backend/src/Recipes.Application/MealPlans/PlanningWorkflow/WorkflowGates.cs`:

**MinSlotsCoveredGate:**
```csharp
public sealed class MinSlotsCoveredGate : IWorkflowGate
{
    public string Name  => "MinSlotsCovered";
    public string Stage => "slot_coverage";

    public GateCheckResult Check(WorkflowGateContext ctx)
    {
        int expected = ctx.ExpectedDays * ctx.MealTypes.Count;
        int actual   = ctx.Draft.Entries.Count;
        return actual >= expected
            ? new(Name, true,  $"{actual}/{expected} slots covered.")
            : new(Name, false, $"Only {actual}/{expected} slots assigned.");
    }
}
```

**RecipeDiversityGate:**
Check that no recipe appears on consecutive days for the same meal type.
```csharp
public sealed class RecipeDiversityGate : IWorkflowGate { ... }
```

**MemberCoverageGate:**
Check that every entry has at least one person assignment.
```csharp
public sealed class MemberCoverageGate : IWorkflowGate { ... }
```

---

## Step 4 — IMealPlanWorkflowEnforcer

Create `Backend/src/Recipes.Application/MealPlans/PlanningWorkflow/IMealPlanWorkflowEnforcer.cs`:

```csharp
public interface IMealPlanWorkflowEnforcer
{
    Task<WorkflowResult> RunAsync(
        RunPlanningWorkflowCommand command,
        HouseholdPlanningProfileDto household,
        CancellationToken ct);
}
```

---

## Step 5 — MealPlanWorkflowEnforcer

Create `Backend/src/Recipes.Infrastructure/AI/Claude/Workflow/MealPlanWorkflowEnforcer.cs`:

```csharp
public sealed class MealPlanWorkflowEnforcer : IMealPlanWorkflowEnforcer
{
    // Constructor: IMealPlanOrchestratorAgent, IEnumerable<IWorkflowGate>
    
    public async Task<WorkflowResult> RunAsync(...)
    {
        // Step 1: run orchestrator
        var result = await _orchestrator.RunAsync(cmd, household, ct);
        if (result.IsError)
            return new WorkflowResult("failed", "orchestration", null, [], result.FirstError.Description);
        
        var draft = result.Value;
        var ctx   = new WorkflowGateContext(draft, cmd.NumberOfDays, cmd.MealTypes, household.Members.Count);
        
        // Step 2: run each gate in order
        var gateResults = new List<GateCheckResult>();
        foreach (var gate in _gates)
        {
            var gr = gate.Check(ctx);
            gateResults.Add(gr);
            if (!gr.Passed)
                return new WorkflowResult("blocked", gate.Stage, draft, gateResults, gr.Message);
        }
        
        // Step 3: handoff
        return new WorkflowResult("pending_approval", "human_approval", draft, gateResults);
    }
}
```

Register gates as `IEnumerable<IWorkflowGate>` (keyed registration pattern):
```csharp
services.AddScoped<IWorkflowGate, MinSlotsCoveredGate>();
services.AddScoped<IWorkflowGate, RecipeDiversityGate>();
services.AddScoped<IWorkflowGate, MemberCoverageGate>();
services.AddScoped<IMealPlanWorkflowEnforcer, MealPlanWorkflowEnforcer>();
```

The enforcer constructor receives `IEnumerable<IWorkflowGate>` — DI resolves all
registered gates in registration order.

---

## Step 6 — Commands and handlers

**RunPlanningWorkflowCommand** (same fields as SuggestMealPlanMultiAgentCommand):
Handler: fetch household → `IMealPlanWorkflowEnforcer.RunAsync()`

**ApprovePlanningWorkflowCommand**:
```csharp
record ApprovePlanningWorkflowCommand(
    MealPlanSuggestionDto Draft,
    bool Approved,
    string? ReviewNotes) : IRequest<ErrorOr<MealPlanSuggestionDto>>;
```
Handler:
- If `!Approved` → `Error.Validation("Workflow.Rejected", reviewNotes)`
- Re-run gates on the submitted draft (in case the client modified it)
- Return draft if all gates pass

---

## Step 7 — Endpoints

```csharp
group.MapPost("/workflow/run", ...)    // → WorkflowResult (200)
group.MapPost("/workflow/approve", ...) // → MealPlanSuggestionDto (200) or 400
```

---

## Step 8 — CCAF doc

Create `Backend/Docs/CCAF/A2-workflow-enforcement.md`.
