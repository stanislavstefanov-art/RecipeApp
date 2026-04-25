# A2 — Workflow Enforcement and Handoff

## What this implements

A `MealPlanWorkflowEnforcer` that wraps the A1 multi-agent orchestrator with three
sequential quality-gate checks and a human-approval handoff step. Two new endpoints
expose the workflow: `POST /api/meal-plans/workflow/run` runs the orchestrator and
evaluates all gates, returning `status: "pending_approval"` when all gates pass or
`status: "blocked"` with the failing gate's detail. `POST /api/meal-plans/workflow/approve`
accepts or rejects the draft plan after human review, re-validating the (possibly
modified) draft before accepting it.

---

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **Workflow Enforcement (1.4)** | `MealPlanWorkflowEnforcer.RunAsync` runs `MinSlotsCoveredGate`, `RecipeDiversityGate`, and `MemberCoverageGate` sequentially after orchestration; a failing gate returns `status: "blocked"` and short-circuits the pipeline |
| **Handoff (1.4)** | Enforcer returns `status: "pending_approval"` with the draft embedded — explicit transfer of control from AI agent to human reviewer; the `/approve` endpoint completes the handoff |
| **Gate-based stage transitions** | Each `IWorkflowGate` checks one invariant on the `MealPlanSuggestionDto`; results are collected and returned so the gate outcome is fully observable by the caller |

---

## Architecture

```
POST /api/meal-plans/workflow/run
          │  MediatR
          ▼
RunPlanningWorkflowHandler
          │  builds HouseholdPlanningProfileDto
          ▼
MealPlanWorkflowEnforcer.RunAsync()
          │
          ├── A1 orchestrator → MealPlanSuggestionDto (or Error → status="failed")
          │
          ├── MinSlotsCoveredGate      stage="slot_coverage"
          │   entries.Count >= days × mealTypes.Count
          │
          ├── RecipeDiversityGate      stage="diversity_review"
          │   no recipe on consecutive days in same meal type
          │
          ├── MemberCoverageGate       stage="member_assignment"
          │   every entry has ≥ 1 assignment
          │
          └── All pass → status="pending_approval"
                    │
                    ▼ human reviews the draft
          POST /api/meal-plans/workflow/approve
                    │  re-runs gates on submitted draft
                    ├── approved=false → Error.Validation (400)
                    └── approved=true  → MealPlanSuggestionDto (200)
```

---

## Key decisions

### Gates in Application, enforcer in Infrastructure
`IWorkflowGate`, `WorkflowGateContext`, and all gate implementations live in
`Recipes.Application.MealPlans.PlanningWorkflow`. They are pure logic — no
infrastructure dependencies — so they belong in Application. `MealPlanWorkflowEnforcer`
lives in Infrastructure because it calls `IMealPlanOrchestratorAgent`, which is an
Infrastructure type.

### `IEnumerable<IWorkflowGate>` DI registration
Gates are registered as multiple `AddScoped<IWorkflowGate, ConcreteGate>()` calls.
The enforcer receives them as `IEnumerable<IWorkflowGate>` and DI resolves all
registrations in order. Adding a new gate requires only a new registration — no changes
to the enforcer.

### Gates re-run on approve
The approve handler re-runs all gates on the submitted draft before accepting it.
This prevents a client from modifying the draft (adding entries, removing assignments)
to bypass a gate that would have blocked the original orchestrator output.

### `WorkflowResult` encodes failure inline
`WorkflowResult.Status` carries `"pending_approval" | "blocked" | "failed"` so the
`/workflow/run` endpoint always returns HTTP 200 with a structured body. HTTP error
codes (400, 404) are reserved for infrastructure failures like household-not-found,
which are returned as `ErrorOr` errors before the workflow even starts.

### `ApprovePlanningWorkflowCommand` includes workflow params
The approve command carries `NumberOfDays` and `MealTypes` alongside the draft so
the gate context can be built accurately. Without them, `MinSlotsCoveredGate` would
have to derive expected slot count from the draft itself, making the check trivially
pass after any client modification.
