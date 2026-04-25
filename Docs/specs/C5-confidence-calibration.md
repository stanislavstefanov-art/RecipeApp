# C5 — Human Review & Confidence Calibration

## Summary

Adds a confidence-calibration loop around the planning workflow. The
`MealPlanWorkflowEnforcer` now reports a `Confidence` value (`high` / `medium` / `low`)
on the `WorkflowResult`, derived from gate pass margins. Every workflow run records a
prediction `(sessionId, source, reportedConfidence)` in an in-memory store. When the
user approves or rejects via `/workflow/approve`, the prediction is resolved with the
actual outcome. A new `GET /api/admin/calibration` endpoint surfaces calibration metrics
(approval rate by confidence bucket), turning the existing one-way `Confidence` flag
into a closed loop.

No existing endpoints are modified in their request shape; only the
`WorkflowResult` JSON gains a new `confidence` field (additive — clients reading other
fields are unaffected).

---

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **5.5 — Human Review & Confidence Calibration** | Workflow runs save `(sessionId, "workflow", confidence)` to `IConfidenceCalibrationStore`; approve/reject mutates the record's `actualOutcome`; `GET /api/admin/calibration` aggregates by confidence bucket — closing the loop between predicted confidence and observed approval rate |

---

## Architecture

```
POST /api/meal-plans/workflow/run
        │  RunPlanningWorkflowHandler
        │  └── MealPlanWorkflowEnforcer.RunAsync()
        │        ├── A1 orchestrator → draft
        │        ├── run gates → all pass / any block
        │        ├── ComputeConfidence(gateResults) → "high" | "medium" | "low"
        │        └── return WorkflowResult { ..., Confidence }
        │  └── _sessionStore.Save(result, ...) → sessionId
        │  └── _calibration.RecordPrediction(sessionId, "workflow", confidence)
        └── WorkflowSessionResult { sessionId, workflowResult }

POST /api/meal-plans/workflow/approve  { sessionId, approved }
        │  ApprovePlanningWorkflowHandler
        │  ├── (existing logic — gates, etc.)
        │  └── _calibration.RecordOutcome(sessionId, approved)
        └── …

GET /api/admin/calibration
        │  GetCalibrationReportQuery
        │  └── _calibration.GetReport()
        └── CalibrationReportDto
            {
              "buckets": [
                { "source": "workflow", "confidence": "high",
                  "total": 12, "approved": 11, "rejected": 1, "unresolved": 0,
                  "approvalRate": 0.916 },
                ...
              ]
            }
```

---

## DTOs and store

```csharp
public sealed record CalibrationRecord(
    Guid PredictionId,        // == sessionId for workflow source
    string Source,            // "workflow"  (extensible later: "import", "critique")
    string ReportedConfidence,// "high" | "medium" | "low"
    bool? ActualOutcome,      // true = approved, false = rejected, null = unresolved
    DateTime CreatedAt,
    DateTime? ResolvedAt);

public interface IConfidenceCalibrationStore
{
    void RecordPrediction(Guid predictionId, string source, string confidence);
    void RecordOutcome(Guid predictionId, bool actualOutcome);
    IReadOnlyList<CalibrationRecord> GetAll();
}

public sealed record CalibrationReportDto(IReadOnlyList<CalibrationBucketDto> Buckets);

public sealed record CalibrationBucketDto(
    string Source,
    string Confidence,
    int Total,
    int Approved,
    int Rejected,
    int Unresolved,
    double ApprovalRate);  // approved / (approved + rejected); 0 if both zero
```

---

## Confidence derivation in the enforcer

When the workflow ends in `pending_approval`, compute confidence from gate margins:

| Condition | Confidence |
|---|---|
| `MinSlotsCoveredGate` reports actual/expected ratio ≥ 1.20 *and* every gate passed | `high` |
| All gates passed with no margin info problematic | `medium` |
| Any gate passed with negative margin or short-circuit on warning | `low` |

For `blocked` or `failed` outcomes, no calibration record is created (the workflow never
reached human review). `WorkflowResult.Confidence` is still set on these outcomes for
visibility but is not recorded.

The exact formula uses the existing `GateCheckResult.Message` text (parses ratio strings
like `"12/10 slots covered."`). If parsing fails, default to `"medium"`.

---

## Files to create

| Path | Purpose |
|---|---|
| `Backend/src/Recipes.Application/Common/AI/IConfidenceCalibrationStore.cs` | Store interface + `CalibrationRecord` |
| `Backend/src/Recipes.Application/Common/AI/CalibrationReportDto.cs` | Report DTOs |
| `Backend/src/Recipes.Application/Admin/GetCalibrationReport/GetCalibrationReportQuery.cs` | Query + handler |
| `Backend/src/Recipes.Infrastructure/AI/Calibration/InMemoryConfidenceCalibrationStore.cs` | Singleton store implementation |
| `Backend/src/Recipes.Api/Endpoints/AdminEndpoints.cs` *(or extend existing)* | `GET /api/admin/calibration` |
| `Backend/Docs/CCAF/C5-confidence-calibration.md` | CCAF documentation |

## Files to modify

| Path | Change |
|---|---|
| `Backend/src/Recipes.Application/MealPlans/PlanningWorkflow/WorkflowResult.cs` | Add `string Confidence` field |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Workflow/MealPlanWorkflowEnforcer.cs` | Compute and set `Confidence` |
| `Backend/src/Recipes.Application/MealPlans/PlanningWorkflow/RunPlanningWorkflowCommand.cs` | Inject store, record prediction after run |
| `Backend/src/Recipes.Application/MealPlans/PlanningWorkflow/ApprovePlanningWorkflowCommand.cs` | Inject store, record outcome |
| `Backend/src/Recipes.Infrastructure/DependencyInjection.cs` | Register `IConfidenceCalibrationStore` (singleton) |
| `Backend/src/Recipes.Api/Program.cs` *(or wherever endpoint groups are mapped)* | Map `MapAdminEndpoints` |

---

## Acceptance criteria

1. `dotnet build Backend/Recipes.sln` passes.
2. `WorkflowResult` JSON includes a `confidence` field (one of `high`, `medium`, `low`).
3. After a successful workflow run that reaches `pending_approval`, a `CalibrationRecord` exists with `actualOutcome = null` and `resolvedAt = null`.
4. After `POST /workflow/approve` with `approved: true`, the matching record's `actualOutcome` is `true` and `resolvedAt` is set.
5. After `POST /workflow/approve` with `approved: false`, the record's `actualOutcome` is `false`.
6. Approving the same `sessionId` twice is idempotent — the second call updates the existing record's `resolvedAt`, no duplicate record created.
7. `GET /api/admin/calibration` returns one bucket per `(source, confidence)` pair present in the store, with `approvalRate = approved / (approved + rejected)` (or `0.0` if denominator is zero).
8. Workflow runs that end in `blocked` or `failed` do NOT create a calibration record.
9. Existing tests in `Recipes.Application.Tests` still pass (51 tests).
