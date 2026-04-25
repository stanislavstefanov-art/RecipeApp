# C5 — Human Review & Confidence Calibration: Implementation Plan

Reference spec: `Docs/specs/C5-confidence-calibration.md`

Build order: store interface + record → report DTOs → query + handler →
store implementation → enforce confidence → wire handlers → DI → endpoint → CCAF doc.

---

## Step 1 — Store interface + CalibrationRecord

Create `Backend/src/Recipes.Application/Common/AI/IConfidenceCalibrationStore.cs`:

```csharp
public sealed record CalibrationRecord(
    Guid PredictionId,
    string Source,
    string ReportedConfidence,
    bool? ActualOutcome,
    DateTime CreatedAt,
    DateTime? ResolvedAt);

public interface IConfidenceCalibrationStore
{
    void RecordPrediction(Guid predictionId, string source, string confidence);
    void RecordOutcome(Guid predictionId, bool actualOutcome);
    IReadOnlyList<CalibrationRecord> GetAll();
}
```

---

## Step 2 — Report DTOs

Create `Backend/src/Recipes.Application/Common/AI/CalibrationReportDto.cs`:

```csharp
public sealed record CalibrationBucketDto(
    string Source,
    string Confidence,
    int Total,
    int Approved,
    int Rejected,
    int Unresolved,
    double ApprovalRate);

public sealed record CalibrationReportDto(IReadOnlyList<CalibrationBucketDto> Buckets);
```

---

## Step 3 — GetCalibrationReport query + handler

Create `Backend/src/Recipes.Application/Admin/GetCalibrationReport/GetCalibrationReportQuery.cs`:

```csharp
public sealed record GetCalibrationReportQuery : IRequest<CalibrationReportDto>;

public sealed class GetCalibrationReportHandler
    : IRequestHandler<GetCalibrationReportQuery, CalibrationReportDto>
{
    private readonly IConfidenceCalibrationStore _store;

    public GetCalibrationReportHandler(IConfidenceCalibrationStore store)
        => _store = store;

    public Task<CalibrationReportDto> Handle(
        GetCalibrationReportQuery request, CancellationToken ct)
    {
        var buckets = _store.GetAll()
            .GroupBy(r => (r.Source, r.ReportedConfidence))
            .Select(g =>
            {
                var approved   = g.Count(r => r.ActualOutcome == true);
                var rejected   = g.Count(r => r.ActualOutcome == false);
                var unresolved = g.Count(r => r.ActualOutcome is null);
                var denom      = approved + rejected;
                return new CalibrationBucketDto(
                    g.Key.Source,
                    g.Key.ReportedConfidence,
                    g.Count(),
                    approved,
                    rejected,
                    unresolved,
                    denom == 0 ? 0.0 : Math.Round((double)approved / denom, 3));
            })
            .OrderBy(b => b.Source).ThenBy(b => b.Confidence)
            .ToList();

        return Task.FromResult(new CalibrationReportDto(buckets));
    }
}
```

---

## Step 4 — InMemoryConfidenceCalibrationStore

Create `Backend/src/Recipes.Infrastructure/AI/Calibration/InMemoryConfidenceCalibrationStore.cs`:

```csharp
public sealed class InMemoryConfidenceCalibrationStore : IConfidenceCalibrationStore
{
    private readonly ConcurrentDictionary<Guid, CalibrationRecord> _records = new();

    public void RecordPrediction(Guid predictionId, string source, string confidence)
        => _records[predictionId] = new CalibrationRecord(
            predictionId, source, confidence,
            ActualOutcome: null,
            CreatedAt: DateTime.UtcNow,
            ResolvedAt: null);

    public void RecordOutcome(Guid predictionId, bool actualOutcome)
    {
        if (_records.TryGetValue(predictionId, out var existing))
            _records[predictionId] = existing with
            {
                ActualOutcome = actualOutcome,
                ResolvedAt    = DateTime.UtcNow
            };
    }

    public IReadOnlyList<CalibrationRecord> GetAll()
        => _records.Values.ToList().AsReadOnly();
}
```

---

## Step 5 — Add Confidence to WorkflowResult

Modify `Backend/src/Recipes.Application/MealPlans/PlanningWorkflow/WorkflowResult.cs`:

```csharp
public sealed record WorkflowResult(
    string Status,
    string CurrentStage,
    MealPlanSuggestionDto? Draft,
    IReadOnlyList<GateCheckResult> GateResults,
    string? BlockedReason = null,
    string Confidence = "medium");   // ← new field with default
```

---

## Step 6 — Compute confidence in MealPlanWorkflowEnforcer

Modify `MealPlanWorkflowEnforcer.RunAsync` — after all gates pass, call:

```csharp
private static string ComputeConfidence(IReadOnlyList<GateCheckResult> gateResults)
{
    // low: any gate message signals a negative margin or warning
    if (gateResults.Any(g =>
            g.Message.Contains("Only", StringComparison.OrdinalIgnoreCase) ||
            g.Message.Contains("warning", StringComparison.OrdinalIgnoreCase)))
        return "low";

    // high: MinSlotsCovered ratio >= 1.20 and all gates passed
    var slotGate = gateResults.FirstOrDefault(g => g.GateName == "MinSlotsCovered");
    if (slotGate is not null)
    {
        // message format: "{actual}/{expected} slots covered."
        var match = Regex.Match(slotGate.Message, @"(\d+)/(\d+)");
        if (match.Success &&
            int.TryParse(match.Groups[1].Value, out var actual) &&
            int.TryParse(match.Groups[2].Value, out var expected) &&
            expected > 0 &&
            (double)actual / expected >= 1.20)
            return "high";
    }

    return "medium";
}
```

The final `return` in `RunAsync` becomes:
```csharp
var confidence = ComputeConfidence(gateResults);
return new WorkflowResult("pending_approval", "human_approval", draft, gateResults,
    Confidence: confidence);
```

For `blocked` and `failed` branches keep `Confidence = "medium"` (default — not recorded).

---

## Step 7 — Record prediction in RunPlanningWorkflowHandler

Modify `RunPlanningWorkflowCommand.cs`:

1. Add `IConfidenceCalibrationStore _calibration` field + constructor param.
2. After `_sessionStore.Save(...)`:

```csharp
if (workflowResult.Status == "pending_approval")
    _calibration.RecordPrediction(sessionId, "workflow", workflowResult.Confidence);
```

---

## Step 8 — Record outcome in ApprovePlanningWorkflowHandler

Modify `ApprovePlanningWorkflowCommand.cs`:

1. Add `IConfidenceCalibrationStore _calibration` field + constructor param.
2. When a session-based approval completes (just before the final `return`):

```csharp
if (request.SessionId.HasValue)
    _calibration.RecordOutcome(request.SessionId.Value, request.Approved);
```

Call it for both approved and rejected paths.

---

## Step 9 — DI registration

Add to `DependencyInjection.cs`:

```csharp
using Recipes.Application.Common.AI;
using Recipes.Infrastructure.AI.Calibration;
// ...
services.AddSingleton<IConfidenceCalibrationStore, InMemoryConfidenceCalibrationStore>();
```

---

## Step 10 — Admin endpoint

Extend `AdminEndpoints.cs`:

```csharp
using Recipes.Application.Common.AI;
// ...
group.MapGet("/calibration", async (ISender sender, CancellationToken ct) =>
{
    var report = await sender.Send(new GetCalibrationReportQuery(), ct);
    return Results.Ok(report);
})
.WithSummary("Return confidence-calibration metrics (approval rate by confidence bucket).");
```

---

## Step 11 — CCAF doc

Create `Backend/Docs/CCAF/C5-confidence-calibration.md` covering:
- What this implements
- CCAF subtopics table (5.5)
- Architecture diagram (run → record → approve → resolve → GET calibration)
- Key decisions (singleton store, prediction ID == sessionId, blocked/failed excluded,
  ratio-based confidence derivation, additive WorkflowResult field)
