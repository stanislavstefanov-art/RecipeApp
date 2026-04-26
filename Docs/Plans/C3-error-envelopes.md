# C3 — Error Envelopes: Implementation Plan

Reference spec: `Docs/specs/C3-error-envelopes.md`

Build order: envelope record + interface → classifier → query + handler →
store implementation → handler modifications (4) → DI → endpoint → CCAF doc.

---

## Step 1 — AiErrorEnvelope + IAiErrorStore

Create `Backend/src/Recipes.Application/Common/AI/AiErrorEnvelope.cs`:

```csharp
public sealed record AiErrorEnvelope(
    Guid Id,
    string Code,
    string Source,
    string Message,
    bool IsRetryable,
    DateTime OccurredAt);

public interface IAiErrorStore
{
    void Record(AiErrorEnvelope envelope);
    IReadOnlyList<AiErrorEnvelope> GetRecent(int limit);
}
```

---

## Step 2 — AiErrorClassifier

Create `Backend/src/Recipes.Application/Common/AI/AiErrorClassifier.cs`:

```csharp
public static class AiErrorClassifier
{
    public static AiErrorEnvelope Classify(Exception ex, string source)
    {
        var msg  = ex.Message;
        var code = Classify(msg);
        return new AiErrorEnvelope(
            Guid.NewGuid(), code, source, msg,
            IsRetryable: code is "api_error" or "timeout",
            OccurredAt: DateTime.UtcNow);
    }

    private static string Classify(string message)
    {
        if (Contains(message, "api key", "missing"))        return "configuration_error";
        if (Contains(message, "timed out", "timeout"))      return "timeout";
        if (Contains(message, "deserializ", "json", "valid")) return "output_validation";
        return "api_error";
    }

    private static bool Contains(string msg, params string[] terms)
        => terms.Any(t => msg.Contains(t, StringComparison.OrdinalIgnoreCase));
}
```

---

## Step 3 — GetAiErrors query + handler

Create `Backend/src/Recipes.Application/Admin/GetAiErrors/GetAiErrorsQuery.cs`:

```csharp
public sealed record GetAiErrorsQuery(int Limit = 100) : IRequest<IReadOnlyList<AiErrorEnvelope>>;

public sealed class GetAiErrorsHandler
    : IRequestHandler<GetAiErrorsQuery, IReadOnlyList<AiErrorEnvelope>>
{
    private readonly IAiErrorStore _store;
    public GetAiErrorsHandler(IAiErrorStore store) => _store = store;

    public Task<IReadOnlyList<AiErrorEnvelope>> Handle(
        GetAiErrorsQuery request, CancellationToken ct)
        => Task.FromResult(_store.GetRecent(Math.Clamp(request.Limit, 1, 1000)));
}
```

---

## Step 4 — InMemoryAiErrorStore

Create `Backend/src/Recipes.Infrastructure/AI/AiErrors/InMemoryAiErrorStore.cs`:

```csharp
public sealed class InMemoryAiErrorStore : IAiErrorStore
{
    private readonly ConcurrentQueue<AiErrorEnvelope> _records = new();

    public void Record(AiErrorEnvelope envelope) => _records.Enqueue(envelope);

    public IReadOnlyList<AiErrorEnvelope> GetRecent(int limit)
        => _records.TakeLast(limit).Reverse().ToList().AsReadOnly();
}
```

---

## Step 5 — CritiqueRecipeHandler

1. Add `IAiErrorStore _aiErrorStore` field + constructor param.
2. Wrap the `CritiqueAsync` call:

```csharp
try
{
    var critique = await _critiqueService.CritiqueAsync(dto, cancellationToken);
    return critique;
}
catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException)
{
    var envelope = AiErrorClassifier.Classify(ex, "recipe-critique");
    _aiErrorStore.Record(envelope);
    return Error.Failure($"AI.{envelope.Code}", envelope.Message);
}
```

---

## Step 6 — ScaleRecipeHandler

Same pattern, `source = "recipe-scaling"`.

---

## Step 7 — ReviewRecipeDraftHandler

Same pattern, `source = "recipe-jury"`. Wrap only the `ReviewAsync` call
(the import step already returns ErrorOr and is handled separately).

---

## Step 8 — SubmitRecipeBatchAnalysisHandler

Read the file first to confirm the handler exists, then wrap `SubmitBatchAsync`.
`source = "recipe-batch"`.

---

## Step 9 — DI registration

```csharp
using Recipes.Infrastructure.AI.AiErrors;
// ...
services.AddSingleton<IAiErrorStore, InMemoryAiErrorStore>();
```

---

## Step 10 — Admin endpoint

```csharp
using Recipes.Application.Admin.GetAiErrors;
// ...
group.MapGet("/ai-errors", async (
    int limit = 100,
    ISender sender = default!,
    CancellationToken ct = default) =>
{
    var records = await sender.Send(new GetAiErrorsQuery(limit), ct);
    return Results.Ok(records);
})
.WithSummary("Return recent AI operation error envelopes (newest first, default 100, max 1000).");
```

---

## Step 11 — CCAF doc

Create `Backend/Docs/CCAF/C3-error-envelopes.md` covering:
- What this implements
- CCAF subtopics table (5.3)
- Classification rules table
- Key decisions (exception filter vs. catch-all, four handlers vs. global middleware,
  IsRetryable semantics, `output_validation` is not retryable)
