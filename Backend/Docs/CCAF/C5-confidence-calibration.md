# C5 — Human Review & Confidence Calibration

## What this implements

Adds a closed-loop confidence-calibration system around the meal-plan
planning workflow. `MealPlanWorkflowEnforcer` computes a `Confidence` value
(`high` / `medium` / `low`) from gate-pass margins and attaches it to
`WorkflowResult`. Every successful workflow run that reaches `pending_approval`
records a prediction `(sessionId, "workflow", confidence)` in an in-memory
store. When the human approves or rejects via `/workflow/approve`, the
prediction is resolved with the actual outcome. A new admin endpoint
`GET /api/admin/calibration` surfaces approval rates per `(source, confidence)`
bucket — turning the one-way confidence flag into an observable closed loop.

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **5.5 — Human Review & Confidence Calibration** | `RunPlanningWorkflowHandler` calls `IConfidenceCalibrationStore.RecordPrediction(sessionId, "workflow", confidence)` after every `pending_approval` run; `ApprovePlanningWorkflowHandler` calls `RecordOutcome(sessionId, approved)` on both approve and reject paths; `GET /api/admin/calibration` aggregates records by `(source, confidence)` bucket and reports `approvalRate = approved / (approved + rejected)` |

## Architecture

```
POST /api/meal-plans/workflow/run
        │  RunPlanningWorkflowHandler
        │  └── MealPlanWorkflowEnforcer.RunAsync()
        │        ├── A1 orchestrator → draft
        │        ├── run gates → all pass
        │        ├── ComputeConfidence(gateResults) → "high" | "medium" | "low"
        │        └── WorkflowResult { ..., Confidence }
        │  └── IWorkflowSessionStore.Save() → sessionId
        │  └── IConfidenceCalibrationStore.RecordPrediction(sessionId, "workflow", confidence)
        └── WorkflowSessionResult { sessionId, workflowResult }

POST /api/meal-plans/workflow/approve  { sessionId, approved }
        │  ApprovePlanningWorkflowHandler
        │  └── IConfidenceCalibrationStore.RecordOutcome(sessionId, approved)
        └── MealPlanSuggestionDto (or Validation error if rejected)

GET /api/admin/calibration
        │  GetCalibrationReportHandler
        │  └── IConfidenceCalibrationStore.GetAll()
        │        group by (source, confidence)
        │        → approvalRate = approved / (approved + rejected)
        └── CalibrationReportDto { Buckets: [...] }
```

## Key decisions

**Prediction ID == sessionId** — the workflow session ID already uniquely
identifies a single planning run, so it doubles as the prediction key with no
extra ID scheme.

**Only `pending_approval` creates a record** — `blocked` and `failed` outcomes
never reach human review, so recording them would pollute calibration with
data that has no matching outcome. `WorkflowResult.Confidence` is still set on
those outcomes for display, but `RecordPrediction` is not called.

**Ratio-based confidence derivation** — `MinSlotsCoveredGate` emits
`"{actual}/{expected} slots covered."` on success. Parsing that ratio and
checking ≥ 1.20 produces an empirically meaningful `"high"` bucket. Parsing
failure defaults to `"medium"` (safe, never throws).

**Additive `WorkflowResult` field** — `string Confidence = "medium"` has a
default value so all existing construction sites compile without changes.

**Singleton store** — mirrors `IWorkflowSessionStore`; both stores hold
in-memory state that should outlive individual request scopes. A future
persistence upgrade would replace both implementations.

**Idempotent `RecordOutcome`** — calling approve twice on the same `sessionId`
overwrites `ResolvedAt` but does not create a duplicate record
(`ConcurrentDictionary` key-indexed).
