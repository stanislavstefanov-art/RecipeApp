# C2 — Escalation

## What this implements

Adds an automatic escalation path to the planning workflow. When the workflow
produces a `pending_approval` result with `Confidence == "low"`, the system
creates an `EscalationRecord` and surfaces `IsEscalated = true` on the response.
Escalated sessions have a mandatory constraint: `POST /workflow/approve` with
`approved: true` returns 422 unless `ReviewNotes` is non-empty — a reviewer
cannot approve a low-confidence plan without explaining the decision. Rejection
without notes is still allowed (the plan was already bad; no justification needed).
`GET /api/admin/escalations` lists records by status.

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **5.2 — Escalation** | `RunPlanningWorkflowHandler` triggers `IEscalationStore.Create(sessionId, "low_confidence")` whenever `Confidence == "low"` and returns `IsEscalated = true`; `ApprovePlanningWorkflowHandler` calls `HasPending(sessionId)` and returns `Error.Validation("Workflow.EscalationRequiresReviewNotes")` if `ReviewNotes` is empty on an approval — enforcing mandatory human oversight for escalated decisions; `Resolve(sessionId, reviewNotes)` is called on both approve and reject paths to close the escalation |

## Architecture

```
POST /api/meal-plans/workflow/run
        │  MealPlanWorkflowEnforcer → { status="pending_approval", Confidence="low" }
        │  RunPlanningWorkflowHandler
        │  ├── _calibration.RecordPrediction(sessionId, "workflow", "low")
        │  ├── _escalation.Create(sessionId, "low_confidence")
        │  └── WorkflowSessionResult { sessionId, workflowResult, IsEscalated=true }
        └── 200 OK

POST /api/meal-plans/workflow/approve  { sessionId, approved:true, reviewNotes:"" }
        │  ApprovePlanningWorkflowHandler
        │  └── _escalation.HasPending(sessionId) = true
        │       && ReviewNotes is empty
        └── 422 Validation error: "Workflow.EscalationRequiresReviewNotes"

POST /api/meal-plans/workflow/approve  { sessionId, approved:true, reviewNotes:"Looks good" }
        │  ApprovePlanningWorkflowHandler
        │  ├── escalation check passes (ReviewNotes provided)
        │  ├── gate loop → all pass
        │  ├── _calibration.RecordOutcome(sessionId, true)
        │  └── _escalation.Resolve(sessionId, "Looks good")
        └── 200 OK + MealPlanSuggestionDto

GET /api/admin/escalations?status=pending
        └── EscalationRecord[]  (pending only)
```

## Key decisions

**Trigger = low confidence** — the escalation criterion is the `Confidence` field
computed by `MealPlanWorkflowEnforcer`. Any plan where gate-pass margins suggest
fewer slots than expected or warning messages were emitted is flagged. High and
medium confidence plans are unaffected.

**Approval requires notes; rejection does not** — rejecting a bad plan is the
"safe default" action and needs no justification. Approving an escalated plan
is the risky override that demands accountability.

**`Resolve` is idempotent for non-escalated sessions** — `InMemoryEscalationStore`
uses `ConcurrentDictionary` keyed by `sessionId`. Calling `Resolve` for a session
that was never escalated is a no-op (key not found). This keeps the approve handler
simple — it always calls `Resolve` without conditionals.

**Singleton store** — mirrors `IWorkflowSessionStore` and `IConfidenceCalibrationStore`;
escalation state must outlive individual request scopes.

**`IsEscalated` on `WorkflowSessionResult`** — surfacing the flag in the run-
response allows the frontend to render a mandatory notes field immediately, rather
than discovering the constraint only when the approve call fails.
