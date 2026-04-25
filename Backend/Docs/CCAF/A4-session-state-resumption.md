# A4 — Session State and Resumption

## What this implements

Every successful `/workflow/run` call now creates a `WorkflowSession` — a persisted
snapshot of the `WorkflowResult` plus the original run parameters (numberOfDays,
mealTypes). The session is identified by a `Guid` and expires after one hour.

Three endpoints expose the pattern:
- `POST /workflow/run` returns `WorkflowSessionResult { sessionId, workflowResult }`.
- `GET /workflow/sessions/{sessionId}` re-hydrates the session (for UI polling or
  display after a browser refresh).
- `POST /workflow/approve` accepts either a `sessionId` (session-resumption path) or
  the full draft payload (direct path); the handler re-hydrates the draft and
  parameters from the store when a session ID is supplied.

---

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **Session State and Resumption (1.7)** | `IWorkflowSessionStore.Save` persists `WorkflowSession` after every `/workflow/run`; `ApprovePlanningWorkflowHandler` calls `Get(sessionId)` to resume the workflow — re-hydrating the draft and original run parameters — without the client re-sending the full payload |

---

## Architecture

```
POST /workflow/run
        │  RunPlanningWorkflowHandler
        │  ├── build household, call enforcer → WorkflowResult
        │  └── _sessionStore.Save(workflowResult, numberOfDays, mealTypes) → sessionId
        └── WorkflowSessionResult { sessionId, workflowResult }

GET /workflow/sessions/{sessionId}
        └── GetWorkflowSessionQuery → IWorkflowSessionStore.Get → WorkflowSession | 404

POST /workflow/approve  ── session path
        │  ApprovePlanningWorkflowHandler
        │  _sessionStore.Get(sessionId) → WorkflowSession
        │  draft = session.WorkflowResult.Draft
        │  numberOfDays = session.NumberOfDays
        │  mealTypes = session.MealTypes
        └── re-run gates → MealPlanSuggestionDto | 400

POST /workflow/approve  ── direct path (backward compat)
        └── uses draft + numberOfDays + mealTypes from request body
```

---

## Key decisions

### Session store as a singleton
`InMemoryWorkflowSessionStore` is a singleton so session state survives across
multiple HTTP requests within the same process lifetime. Gates and enforcer are scoped
(per-request) — the store must outlive them.

### Session saved regardless of workflow outcome
`RunPlanningWorkflowHandler` saves the session even when `WorkflowResult.Status` is
`"blocked"` or `"failed"`. This enables the client to retrieve the full failure detail
from `GET /workflow/sessions/{id}` without having to re-POST. The approve handler
guards against approving a null-draft session.

### `WorkflowSessionResult` wraps the existing DTO
`WorkflowSessionResult(Guid SessionId, WorkflowResult WorkflowResult)` is an additive
wrapper — the `WorkflowResult` shape is unchanged, so clients that read only
`workflowResult` from the response are unaffected by the session layer.

### Approve accepts both paths
`ApproveWorkflowRequest.SessionId` is nullable. When present, the handler loads the
session and ignores `Draft`, `NumberOfDays`, and `MealTypes` from the request body.
When absent, the handler uses the body fields (original A2 behaviour). This keeps
backward compatibility while adding the resumption path.

### Expiry-on-read, cap-on-write
Expired sessions are removed lazily when `Get` is called, not on a background timer.
Overflow is handled in `Save`: if the store exceeds 500 entries, the oldest entries
(by `CreatedAt`) are evicted. This keeps the implementation simple and deterministic
for a demo service.
