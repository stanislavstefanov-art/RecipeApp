# A4 — Session State and Resumption

## Summary

Persists the output of each `/workflow/run` call as a `WorkflowSession` identified by
a `Guid`. The session carries the `WorkflowResult`, original run parameters, and an
expiry timestamp. Two additions to the existing workflow endpoints:

- `POST /api/meal-plans/workflow/run` now returns `WorkflowSessionResult` (adds
  `sessionId` alongside the existing `WorkflowResult`).
- `POST /api/meal-plans/workflow/approve` accepts either the full draft payload (as
  before) **or** just `{ sessionId, approved, reviewNotes }` — the handler re-hydrates
  the draft and parameters from the session store.
- `GET /api/meal-plans/workflow/sessions/{sessionId}` — retrieves a saved session
  (useful for UI polling or display after browser refresh).

No existing endpoints are modified.

---

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **Session State and Resumption (1.7)** | `IWorkflowSessionStore.Save` persists `WorkflowSession` after every successful `/workflow/run`; `ApprovePlanningWorkflowHandler` re-hydrates the session via `Get(sessionId)` to resume the workflow without the client re-sending the full draft |

---

## Architecture

```
POST /api/meal-plans/workflow/run
        │  RunPlanningWorkflowHandler
        │  ├── build household profile
        │  ├── IMealPlanWorkflowEnforcer.RunAsync → WorkflowResult
        │  └── IWorkflowSessionStore.Save(result, numberOfDays, mealTypes)
        │                                          → Guid sessionId
        └── WorkflowSessionResult { sessionId, workflowResult }

GET /api/meal-plans/workflow/sessions/{sessionId}
        │  GetWorkflowSessionQuery
        └── IWorkflowSessionStore.Get(sessionId) → WorkflowSession | 404

POST /api/meal-plans/workflow/approve
        │  ApprovePlanningWorkflowHandler
        ├── if sessionId ──▶ IWorkflowSessionStore.Get(sessionId)
        │                    extract draft + numberOfDays + mealTypes
        └── if no sessionId ─▶ use draft + params from request body
```

---

## WorkflowSession

```csharp
record WorkflowSession(
    Guid SessionId,
    WorkflowResult WorkflowResult,
    int NumberOfDays,
    IReadOnlyList<int> MealTypes,
    DateTime CreatedAt,
    DateTime ExpiresAt);

record WorkflowSessionResult(Guid SessionId, WorkflowResult WorkflowResult);
```

---

## IWorkflowSessionStore

```csharp
interface IWorkflowSessionStore
{
    Guid Save(WorkflowResult result, int numberOfDays, IReadOnlyList<int> mealTypes);
    WorkflowSession? Get(Guid sessionId);  // null if not found or expired
}
```

`InMemoryWorkflowSessionStore` — singleton, `ConcurrentDictionary`, sessions expire
after 1 hour, capped at 500 entries (evict oldest on overflow).

---

## Files to create

| Path | Purpose |
|---|---|
| `Backend/src/Recipes.Application/MealPlans/PlanningWorkflow/WorkflowSession.cs` | `WorkflowSession`, `WorkflowSessionResult` records |
| `Backend/src/Recipes.Application/MealPlans/PlanningWorkflow/IWorkflowSessionStore.cs` | Store interface |
| `Backend/src/Recipes.Application/MealPlans/PlanningWorkflow/GetWorkflowSessionQuery.cs` | Query + handler |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Workflow/InMemoryWorkflowSessionStore.cs` | In-memory implementation |
| `Backend/Docs/CCAF/A4-session-state-resumption.md` | CCAF documentation |

## Files to modify

| Path | Change |
|---|---|
| `Backend/src/Recipes.Application/MealPlans/PlanningWorkflow/RunPlanningWorkflowCommand.cs` | Return `ErrorOr<WorkflowSessionResult>`; inject + call store |
| `Backend/src/Recipes.Application/MealPlans/PlanningWorkflow/ApprovePlanningWorkflowCommand.cs` | Add `Guid? SessionId`; inject + load from store |
| `Backend/src/Recipes.Api/Endpoints/MealPlansEndpoints.cs` | Update `/workflow/run` response, add session GET, update approve request |
| `Backend/src/Recipes.Infrastructure/DependencyInjection.cs` | Register store as singleton |

---

## Acceptance criteria

1. `dotnet build Backend/Recipes.sln` passes.
2. `POST /workflow/run` response contains `sessionId` and `workflowResult`.
3. `GET /workflow/sessions/{sessionId}` returns the saved session.
4. `POST /workflow/approve` with `{ sessionId, approved: true }` re-hydrates session and returns `MealPlanSuggestionDto`.
5. `POST /workflow/approve` with `{ sessionId, approved: false }` returns 400.
6. `GET /workflow/sessions/{unknownId}` returns 404.
