# A4 — Session State and Resumption: Implementation Plan

Reference spec: `Docs/specs/A4-session-state-resumption.md`

Build order: session DTOs → store interface → query → store implementation →
update run handler → update approve handler → endpoints → DI → CCAF doc.

---

## Step 1 — WorkflowSession.cs

Create `Backend/src/Recipes.Application/MealPlans/PlanningWorkflow/WorkflowSession.cs`:

```csharp
public sealed record WorkflowSession(
    Guid SessionId,
    WorkflowResult WorkflowResult,
    int NumberOfDays,
    IReadOnlyList<int> MealTypes,
    DateTime CreatedAt,
    DateTime ExpiresAt);

public sealed record WorkflowSessionResult(
    Guid SessionId,
    WorkflowResult WorkflowResult);
```

---

## Step 2 — IWorkflowSessionStore.cs

```csharp
public interface IWorkflowSessionStore
{
    Guid Save(WorkflowResult result, int numberOfDays, IReadOnlyList<int> mealTypes);
    WorkflowSession? Get(Guid sessionId);
}
```

---

## Step 3 — GetWorkflowSessionQuery.cs

Command + handler in one file:
- `GetWorkflowSessionQuery(Guid SessionId) : IRequest<ErrorOr<WorkflowSession>>`
- Handler: `_store.Get(sessionId)` → NotFound or the session

---

## Step 4 — InMemoryWorkflowSessionStore.cs

Singleton. `ConcurrentDictionary<Guid, WorkflowSession>`.
`Save`: generates `Guid.NewGuid()`, sets `ExpiresAt = DateTime.UtcNow + 1h`,
stores, evicts oldest entries if Count > 500.
`Get`: returns null if key missing OR `session.ExpiresAt < DateTime.UtcNow`.

---

## Step 5 — Update RunPlanningWorkflowCommand.cs

- Change `IRequest<ErrorOr<WorkflowResult>>` → `IRequest<ErrorOr<WorkflowSessionResult>>`
- Inject `IWorkflowSessionStore`
- After enforcer returns the `WorkflowResult`: `store.Save(result, request.NumberOfDays, request.MealTypes)`
- Return `WorkflowSessionResult(sessionId, workflowResult)`

---

## Step 6 — Update ApprovePlanningWorkflowCommand.cs

- Add `Guid? SessionId` to the command record
- Inject `IWorkflowSessionStore`
- If `SessionId` is set: `store.Get(id)` → NotFound or extract draft/params
- Re-run gates (existing logic, unchanged)

---

## Step 7 — Endpoints

- `/workflow/run` returns `WorkflowSessionResult` (shape change, sessionId added)
- Add `GET /workflow/sessions/{sessionId}`
- Update `ApproveWorkflowRequest` to add optional `SessionId`; make `Draft`,
  `NumberOfDays`, `MealTypes` nullable (defaults 0/null) for the session path

---

## Step 8 — DI

```csharp
services.AddSingleton<IWorkflowSessionStore, InMemoryWorkflowSessionStore>();
```

---

## Step 9 — CCAF doc

Create `Backend/Docs/CCAF/A4-session-state-resumption.md`.
