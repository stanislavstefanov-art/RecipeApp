# C2 — Escalation: Implementation Plan

Reference spec: `Docs/specs/C2-escalation.md`

Build order: store interface → query + handler → store implementation →
WorkflowSessionResult change → RunPlanningWorkflow changes →
ApprovePlanningWorkflow changes → DI → endpoint → CCAF doc.

---

## Step 1 — IEscalationStore + EscalationRecord

Create `Backend/src/Recipes.Application/Common/AI/IEscalationStore.cs`:

```csharp
public sealed record EscalationRecord(
    Guid EscalationId,
    Guid SessionId,
    string Reason,
    DateTime CreatedAt,
    DateTime? ResolvedAt,
    string? ReviewNotes);

public interface IEscalationStore
{
    void Create(Guid sessionId, string reason);
    bool HasPending(Guid sessionId);
    void Resolve(Guid sessionId, string? reviewNotes);
    IReadOnlyList<EscalationRecord> GetAll(string status); // "all"|"pending"|"resolved"
}
```

---

## Step 2 — GetEscalations query + handler

Create `Backend/src/Recipes.Application/Admin/GetEscalations/GetEscalationsQuery.cs`:

```csharp
public sealed record GetEscalationsQuery(string Status = "all")
    : IRequest<IReadOnlyList<EscalationRecord>>;

public sealed class GetEscalationsHandler
    : IRequestHandler<GetEscalationsQuery, IReadOnlyList<EscalationRecord>>
{
    private readonly IEscalationStore _store;
    public GetEscalationsHandler(IEscalationStore store) => _store = store;

    public Task<IReadOnlyList<EscalationRecord>> Handle(
        GetEscalationsQuery request, CancellationToken ct)
        => Task.FromResult(_store.GetAll(request.Status));
}
```

---

## Step 3 — InMemoryEscalationStore

Create `Backend/src/Recipes.Infrastructure/AI/Escalation/InMemoryEscalationStore.cs`:

```csharp
public sealed class InMemoryEscalationStore : IEscalationStore
{
    private readonly ConcurrentDictionary<Guid, EscalationRecord> _records = new();

    public void Create(Guid sessionId, string reason)
        => _records[sessionId] = new EscalationRecord(
            Guid.NewGuid(), sessionId, reason,
            CreatedAt: DateTime.UtcNow,
            ResolvedAt: null, ReviewNotes: null);

    public bool HasPending(Guid sessionId)
        => _records.TryGetValue(sessionId, out var r) && r.ResolvedAt is null;

    public void Resolve(Guid sessionId, string? reviewNotes)
    {
        if (_records.TryGetValue(sessionId, out var existing))
            _records[sessionId] = existing with
            {
                ResolvedAt  = DateTime.UtcNow,
                ReviewNotes = reviewNotes
            };
    }

    public IReadOnlyList<EscalationRecord> GetAll(string status)
    {
        var all = _records.Values.ToList();
        return status switch
        {
            "pending"  => all.Where(r => r.ResolvedAt is null).ToList().AsReadOnly(),
            "resolved" => all.Where(r => r.ResolvedAt is not null).ToList().AsReadOnly(),
            _          => all.AsReadOnly(),
        };
    }
}
```

---

## Step 4 — WorkflowSessionResult change

Modify `WorkflowSession.cs`:

```csharp
public sealed record WorkflowSessionResult(
    Guid SessionId,
    WorkflowResult WorkflowResult,
    bool IsEscalated = false);
```

---

## Step 5 — RunPlanningWorkflowHandler

1. Add `IEscalationStore _escalationStore` field + constructor param.
2. After the existing calibration `RecordPrediction` call:

```csharp
var isEscalated = false;
if (workflowResult.Status == "pending_approval" && workflowResult.Confidence == "low")
{
    _escalationStore.Create(sessionId, "low_confidence");
    isEscalated = true;
}

return new WorkflowSessionResult(sessionId, workflowResult, isEscalated);
```

Remove the old unconditional `return new WorkflowSessionResult(sessionId, workflowResult)`.

---

## Step 6 — ApprovePlanningWorkflowHandler

1. Add `IEscalationStore _escalationStore` field + constructor param.
2. After the session is loaded (draft and mealTypes are resolved), before the gate loop:

```csharp
if (request.Approved &&
    request.SessionId.HasValue &&
    _escalationStore.HasPending(request.SessionId.Value) &&
    string.IsNullOrWhiteSpace(request.ReviewNotes))
{
    return Task.FromResult<ErrorOr<MealPlanSuggestionDto>>(
        Error.Validation("Workflow.EscalationRequiresReviewNotes",
            "A review note is required to approve an escalated plan."));
}
```

3. After `_calibration.RecordOutcome(...)` on both resolve paths (approve + reject),
   add:

```csharp
if (request.SessionId.HasValue)
    _escalationStore.Resolve(request.SessionId.Value, request.ReviewNotes);
```

Note: `Resolve` is idempotent for non-escalated sessions (key not found = no-op).

---

## Step 7 — DI registration

```csharp
using Recipes.Infrastructure.AI.Escalation;
// ...
services.AddSingleton<IEscalationStore, InMemoryEscalationStore>();
```

---

## Step 8 — Admin endpoint

Extend `AdminEndpoints.cs`:

```csharp
using Recipes.Application.Admin.GetEscalations;
// ...
group.MapGet("/escalations", async (
    string status = "all",
    ISender sender = default!,
    CancellationToken ct = default) =>
{
    var records = await sender.Send(new GetEscalationsQuery(status), ct);
    return Results.Ok(records);
})
.WithSummary("Return escalation records. status=all|pending|resolved (default: all).");
```

---

## Step 9 — CCAF doc

Create `Backend/Docs/CCAF/C2-escalation.md` covering:
- What this implements
- CCAF subtopics table (5.2)
- Architecture diagram
- Key decisions (trigger = low confidence, mandatory ReviewNotes, reject without notes OK,
  Resolve is idempotent, keyed by sessionId not EscalationId)
