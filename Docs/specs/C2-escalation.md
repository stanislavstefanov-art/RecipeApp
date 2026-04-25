# C2 — Escalation

## Summary

Adds an automatic escalation path to the planning workflow. When the workflow
produces a `pending_approval` result with `Confidence == "low"`, the system
creates an `EscalationRecord` in a singleton in-memory store. Escalated sessions
have a mandatory constraint: `POST /workflow/approve` rejects the request with 422
unless `ReviewNotes` is non-empty — a reviewer cannot approve a low-confidence plan
without explaining why. Resolution (approve or reject) closes the escalation record.
`WorkflowSessionResult` gains an `IsEscalated` flag so clients know to surface the
mandatory notes field. `GET /api/admin/escalations` lists all records (pending and
resolved).

No existing endpoints are modified in their request shape.

---

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **5.2 — Escalation** | `RunPlanningWorkflowHandler` triggers `IEscalationStore.Create(sessionId, "low_confidence")` whenever `Confidence == "low"`; `ApprovePlanningWorkflowHandler` checks `IEscalationStore.HasPending(sessionId)` and returns `Error.Validation` if `ReviewNotes` is empty — enforcing mandatory human oversight for escalated decisions; `GET /api/admin/escalations` surfaces the full escalation audit trail |

---

## Architecture

```
POST /api/meal-plans/workflow/run
        │  RunPlanningWorkflowHandler
        │  └── MealPlanWorkflowEnforcer → WorkflowResult { Status="pending_approval", Confidence="low" }
        │  └── _calibration.RecordPrediction(...)
        │  └── if Confidence == "low":
        │        _escalation.Create(sessionId, "low_confidence")
        │        isEscalated = true
        └── WorkflowSessionResult { sessionId, workflowResult, IsEscalated=true }

POST /api/meal-plans/workflow/approve  { sessionId, approved, reviewNotes }
        │  ApprovePlanningWorkflowHandler
        │  └── if _escalation.HasPending(sessionId) && string.IsNullOrWhiteSpace(ReviewNotes):
        │        return Error.Validation("Workflow.EscalationRequiresReviewNotes", ...)
        │  └── (existing gate + calibration logic)
        │  └── _escalation.Resolve(sessionId, reviewNotes)
        └── MealPlanSuggestionDto or Error

GET /api/admin/escalations?status=all|pending|resolved
        │  GetEscalationsQuery
        │  └── _escalation.GetAll(status)
        └── IReadOnlyList<EscalationRecord>
```

---

## EscalationRecord and store

```csharp
public sealed record EscalationRecord(
    Guid EscalationId,
    Guid SessionId,
    string Reason,            // "low_confidence"
    DateTime CreatedAt,
    DateTime? ResolvedAt,
    string? ReviewNotes);     // null until resolved

public interface IEscalationStore
{
    void   Create(Guid sessionId, string reason);
    bool   HasPending(Guid sessionId);
    void   Resolve(Guid sessionId, string? reviewNotes);
    IReadOnlyList<EscalationRecord> GetAll(string status); // "all" | "pending" | "resolved"
}
```

---

## WorkflowSessionResult change (additive)

```csharp
public sealed record WorkflowSessionResult(
    Guid SessionId,
    WorkflowResult WorkflowResult,
    bool IsEscalated = false);   // ← new, defaults to false
```

---

## Approve constraint

When the session has a pending escalation:
- `approved = false` with empty `ReviewNotes` → still allowed (rejection without notes is acceptable)
- `approved = true` with empty `ReviewNotes` → `Error.Validation("Workflow.EscalationRequiresReviewNotes", "A review note is required to approve an escalated plan.")`
- `approved = true` with non-empty `ReviewNotes` → proceeds normally; escalation resolved

---

## Files to create

| Path | Purpose |
|---|---|
| `Backend/src/Recipes.Application/Common/AI/IEscalationStore.cs` | Store interface + `EscalationRecord` |
| `Backend/src/Recipes.Application/Admin/GetEscalations/GetEscalationsQuery.cs` | Query + handler |
| `Backend/src/Recipes.Infrastructure/AI/Escalation/InMemoryEscalationStore.cs` | Singleton store |
| `Backend/Docs/CCAF/C2-escalation.md` | CCAF documentation |

## Files to modify

| Path | Change |
|---|---|
| `Backend/src/Recipes.Application/MealPlans/PlanningWorkflow/WorkflowSession.cs` | Add `bool IsEscalated = false` to `WorkflowSessionResult` |
| `Backend/src/Recipes.Application/MealPlans/PlanningWorkflow/RunPlanningWorkflowCommand.cs` | Inject store; create escalation + set `IsEscalated` when `Confidence == "low"` |
| `Backend/src/Recipes.Application/MealPlans/PlanningWorkflow/ApprovePlanningWorkflowCommand.cs` | Inject store; enforce `ReviewNotes`; resolve escalation |
| `Backend/src/Recipes.Infrastructure/DependencyInjection.cs` | Register `IEscalationStore` (singleton) |
| `Backend/src/Recipes.Api/Endpoints/AdminEndpoints.cs` | Add `GET /api/admin/escalations` |

---

## Acceptance criteria

1. `dotnet build Backend/Recipes.sln` passes.
2. A `pending_approval` run with `Confidence == "low"` returns `IsEscalated = true` and creates an `EscalationRecord` with `ResolvedAt = null`.
3. `POST /workflow/approve` with `approved: true` and empty `ReviewNotes` on an escalated session returns 422.
4. `POST /workflow/approve` with `approved: true` and non-empty `ReviewNotes` on an escalated session succeeds and resolves the escalation record.
5. `POST /workflow/approve` with `approved: false` and empty `ReviewNotes` on an escalated session succeeds (rejection without notes is allowed).
6. Non-escalated sessions (Confidence `high` or `medium`) are unaffected — no escalation record, no `ReviewNotes` requirement.
7. `GET /api/admin/escalations` returns all escalation records.
8. Existing `Recipes.Application.Tests` pass (51 tests).
