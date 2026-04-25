# A2 — Workflow Enforcement and Handoff

## Summary

Adds a `MealPlanWorkflowEnforcer` that wraps the A1 multi-agent orchestrator with
explicit gate checks between stages and a human-approval handoff step.

Two new endpoints:
- `POST /api/meal-plans/workflow/run` — runs the orchestrator, evaluates quality gates,
  returns a `WorkflowResult` (either `pending_approval` if all gates pass, or `blocked`
  with the failing gate detail)
- `POST /api/meal-plans/workflow/approve` — accepts the draft plan for final approval or
  rejects it with notes

No existing endpoints are modified.

---

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **Workflow Enforcement (1.4)** | `MealPlanWorkflowEnforcer` runs three `IWorkflowGate` checks after orchestration; a failing gate returns `status: "blocked"` and halts the pipeline |
| **Handoff (1.4)** | Enforcer returns `status: "pending_approval"` and defers to the `/approve` endpoint — explicit transfer of control from AI agent to human reviewer |
| **Gate-based stage transitions** | `MinSlotsCoveredGate`, `RecipeDiversityGate`, `MemberCoverageGate` each check one invariant; results are returned to the caller so the gate outcome is observable |

---

## Architecture

```
POST /api/meal-plans/workflow/run
          │  MediatR
          ▼
RunPlanningWorkflowHandler
          │  builds HouseholdPlanningProfileDto
          ▼
IMealPlanWorkflowEnforcer.RunAsync()
          │
          ├── Step 1: Run A1 orchestrator → MealPlanSuggestionDto (or Error)
          │
          ├── Gate 1: MinSlotsCoveredGate
          │   pass: entries.Count == days × mealTypes.Count
          │   fail: status="blocked", stage="slot_coverage"
          │
          ├── Gate 2: RecipeDiversityGate
          │   pass: no recipe used on consecutive days in the same meal type
          │   fail: status="blocked", stage="diversity_review"
          │
          ├── Gate 3: MemberCoverageGate
          │   pass: every entry has at least one assignment
          │   fail: status="blocked", stage="member_assignment"
          │
          └── All gates pass → status="pending_approval"
                    │
                    ▼ (human reviews the draft)
          POST /api/meal-plans/workflow/approve
                    │  { draft, approved, reviewNotes }
                    ▼
          ApprovePlanningWorkflowHandler
                    │  re-runs gates on the submitted draft
                    ├── approved=false → Error (rejected)
                    └── approved=true  → MealPlanSuggestionDto
```

---

## WorkflowResult

```csharp
record WorkflowResult(
    string Status,           // "pending_approval" | "blocked" | "failed"
    string CurrentStage,     // "pending_approval" | "slot_coverage" | "diversity_review" | etc.
    MealPlanSuggestionDto? Draft,
    IReadOnlyList<GateCheckResult> GateResults,
    string? BlockedReason = null);

record GateCheckResult(string GateName, bool Passed, string Message);
```

---

## Gate definitions

| Gate | Stage | Pass condition |
|---|---|---|
| `MinSlotsCoveredGate` | `slot_coverage` | `entries.Count == days × mealTypes.Count` |
| `RecipeDiversityGate` | `diversity_review` | No recipe appears on consecutive days within the same meal type |
| `MemberCoverageGate` | `member_assignment` | Every entry has ≥ 1 assignment |

Gates run sequentially; the first failure short-circuits and returns `blocked`.
All gate results are included in the response regardless of pass/fail.

---

## Handoff semantics

The `pending_approval` status is the formal handoff point. The AI agent has finished;
the workflow now waits for a human decision. The draft plan is embedded in the response
so the client can display it for review. The approve endpoint re-validates the (possibly
modified) draft before accepting it, ensuring the handoff cannot bypass the gates.

---

## Files to create

| Path | Purpose |
|---|---|
| `Backend/src/Recipes.Application/MealPlans/PlanningWorkflow/WorkflowResult.cs` | `WorkflowResult`, `GateCheckResult` DTOs |
| `Backend/src/Recipes.Application/MealPlans/PlanningWorkflow/IWorkflowGate.cs` | `IWorkflowGate` interface + `WorkflowGateContext` |
| `Backend/src/Recipes.Application/MealPlans/PlanningWorkflow/WorkflowGates.cs` | `MinSlotsCoveredGate`, `RecipeDiversityGate`, `MemberCoverageGate` |
| `Backend/src/Recipes.Application/MealPlans/PlanningWorkflow/RunPlanningWorkflowCommand.cs` | Command + handler |
| `Backend/src/Recipes.Application/MealPlans/PlanningWorkflow/ApprovePlanningWorkflowCommand.cs` | Command + handler |
| `Backend/src/Recipes.Application/MealPlans/PlanningWorkflow/IMealPlanWorkflowEnforcer.cs` | Enforcer interface |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Workflow/MealPlanWorkflowEnforcer.cs` | Enforcer implementation |
| `Backend/Docs/CCAF/A2-workflow-enforcement.md` | CCAF documentation |

## Files to modify

| Path | Change |
|---|---|
| `Backend/src/Recipes.Api/Endpoints/MealPlansEndpoints.cs` | Add two `/workflow/*` endpoints |
| `Backend/src/Recipes.Infrastructure/DependencyInjection.cs` | Register enforcer |

---

## Acceptance criteria

1. `dotnet build Backend/Recipes.sln` passes.
2. `POST /api/meal-plans/workflow/run` returns `status: "pending_approval"` with a valid draft and three passing gate results when the orchestrator succeeds.
3. If the orchestrator fails (e.g., no recipes), returns `status: "failed"`.
4. `POST /api/meal-plans/workflow/approve` with `approved: false` returns 400.
5. `POST /api/meal-plans/workflow/approve` with `approved: true` and a valid draft returns the `MealPlanSuggestionDto`.
6. Existing `/api/meal-plans/suggest` and `/api/meal-plans/suggest/multi-agent` unchanged.
