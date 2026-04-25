# C6 — AI Output Provenance: Implementation Plan

Reference spec: `Docs/specs/C6-provenance.md`

Build order: store interface + record → query + handler → store implementation →
DTO changes → service changes → DI → endpoint → CCAF doc.

---

## Step 1 — Store interface + ProvenanceRecord

Create `Backend/src/Recipes.Application/Common/AI/IProvenanceStore.cs`:

```csharp
public sealed record ProvenanceRecord(
    Guid Id,
    string Feature,
    string Model,
    string PromptVersion,
    DateTime GeneratedAt);

public interface IProvenanceStore
{
    Guid Record(string feature, string model, string promptVersion);
    IReadOnlyList<ProvenanceRecord> GetRecent(int limit);
}
```

---

## Step 2 — GetProvenance query + handler

Create `Backend/src/Recipes.Application/Admin/GetProvenance/GetProvenanceQuery.cs`:

```csharp
public sealed record GetProvenanceQuery(int Limit = 100) : IRequest<IReadOnlyList<ProvenanceRecord>>;

public sealed class GetProvenanceHandler
    : IRequestHandler<GetProvenanceQuery, IReadOnlyList<ProvenanceRecord>>
{
    private readonly IProvenanceStore _store;
    public GetProvenanceHandler(IProvenanceStore store) => _store = store;

    public Task<IReadOnlyList<ProvenanceRecord>> Handle(
        GetProvenanceQuery request, CancellationToken ct)
        => Task.FromResult(_store.GetRecent(Math.Clamp(request.Limit, 1, 1000)));
}
```

---

## Step 3 — InMemoryProvenanceStore

Create `Backend/src/Recipes.Infrastructure/AI/Provenance/InMemoryProvenanceStore.cs`:

```csharp
public sealed class InMemoryProvenanceStore : IProvenanceStore
{
    private readonly ConcurrentQueue<ProvenanceRecord> _records = new();

    public Guid Record(string feature, string model, string promptVersion)
    {
        var id = Guid.NewGuid();
        _records.Enqueue(new ProvenanceRecord(id, feature, model, promptVersion, DateTime.UtcNow));
        return id;
    }

    public IReadOnlyList<ProvenanceRecord> GetRecent(int limit)
        => _records.TakeLast(limit).Reverse().ToList().AsReadOnly();
}
```

Note: `ConcurrentQueue` preserves insertion order. `TakeLast(limit)` then `Reverse()` gives
newest-first ordering for the API response.

---

## Step 4 — DTO changes (additive)

Add `Guid ProvenanceId` as the last field in each record:

**`RecipeCritiqueDto.cs`**:
```csharp
public sealed record RecipeCritiqueDto(
    IReadOnlyList<CritiqueDimensionDto> Dimensions,
    string Summary,
    string Confidence,
    bool NeedsReview,
    Guid ProvenanceId);   // ← new
```

**`ScaledRecipeDto.cs`**:
```csharp
public sealed record ScaledRecipeDto(
    Guid RecipeId,
    string Name,
    int FromServings,
    int ToServings,
    IReadOnlyList<ScaledIngredientDto> Ingredients,
    int AttemptsTaken,
    Guid ProvenanceId);   // ← new
```

**`RecipeDraftReviewDto.cs`**:
```csharp
public sealed record RecipeDraftReviewDto(
    ImportedRecipeDto Draft,
    string Consensus,
    IReadOnlyList<JurorVerdictDto> JurorVerdicts,
    Guid ProvenanceId);   // ← new
```

**`BatchAnalysisDto.cs`** — `BatchSubmissionDto`:
```csharp
public sealed record BatchSubmissionDto(
    string BatchId,
    string Status,
    int RequestCount,
    Guid ProvenanceId);   // ← new
```

---

## Step 5 — Prompt version helper

Each service needs a one-time SHA-256 computation. Extract a static helper to avoid
duplicating the import:

In each service constructor (after `_systemPrompt` is assigned):
```csharp
_promptVersion = ComputePromptVersion(_systemPrompt);
```

And add the static method once per class:
```csharp
private static string ComputePromptVersion(string prompt)
{
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(prompt));
    return Convert.ToHexString(hash)[..8].ToLowerInvariant();
}
```

Required using: `using System.Security.Cryptography;`

---

## Step 6 — ClaudeRecipeCritiqueService

1. Add `IProvenanceStore _provenanceStore` field + constructor param.
2. Add `private readonly string _promptVersion;` + assign in constructor.
3. In `CritiqueAsync`, just before the final `return`:
   ```csharp
   var provenanceId = _provenanceStore.Record("recipe-critique", _options.Model, _promptVersion);
   return new RecipeCritiqueDto(dimensions, summary, confidence, needsReview, provenanceId);
   ```

---

## Step 7 — ClaudeRecipeScalingService

1. Add `IProvenanceStore _provenanceStore` field + constructor param.
2. Add `_promptVersion` + assign in constructor.
3. In `ScaleAsync`, just before the final successful `return`:
   ```csharp
   var provenanceId = _provenanceStore.Record("recipe-scaling", _options.Model, _promptVersion);
   return new ScaledRecipeDto(dto.Id, dto.Name, fromServings, toServings, scaledIngredients,
       attemptsTaken, provenanceId);
   ```

---

## Step 8 — ClaudeRecipeDraftReviewService

1. Add `IProvenanceStore _provenanceStore` field + constructor param.
2. Add `_promptVersion` + assign in constructor (based on the first reviewer's system prompt
   concatenation — or a combined hash of all three reviewer prompts).
3. In `ReviewAsync`, just before the final `return`:
   ```csharp
   var provenanceId = _provenanceStore.Record("recipe-jury", _options.Model, _promptVersion);
   return new RecipeDraftReviewDto(draft, consensus, jurorVerdicts, provenanceId);
   ```

For the combined hash: concatenate all three reviewer prompts into one string before hashing.

---

## Step 9 — ClaudeRecipeBatchAnalysisService

1. Add `IProvenanceStore _provenanceStore` field + constructor param.
2. Add `_promptVersion` + assign in constructor (hash of the static `CachedSystemBlocks` text).
3. In `SubmitBatchAsync`, just before the final `return`:
   ```csharp
   var provenanceId = _provenanceStore.Record("recipe-batch", _options.Model, _promptVersion);
   return new BatchSubmissionDto(batchId, status, requestCount, provenanceId);
   ```

---

## Step 10 — DI registration

```csharp
using Recipes.Infrastructure.AI.Provenance;
// ...
services.AddSingleton<IProvenanceStore, InMemoryProvenanceStore>();
```

---

## Step 11 — Admin endpoint

Extend `AdminEndpoints.cs`:

```csharp
using Recipes.Application.Admin.GetProvenance;
using Recipes.Application.Common.AI;
// ...
group.MapGet("/provenance", async (
    int limit = 100,
    ISender sender = default!,
    CancellationToken ct = default) =>
{
    var records = await sender.Send(new GetProvenanceQuery(limit), ct);
    return Results.Ok(records);
})
.WithSummary("Return recent AI output provenance records.");
```

---

## Step 12 — CCAF doc

Create `Backend/Docs/CCAF/C6-provenance.md` covering:
- What this implements
- CCAF subtopics table (5.6)
- Architecture diagram
- Key decisions (prompt hash for version, singleton queue store, 4 features covered,
  ProvenanceId embedded in response DTOs, pre-existing services untouched)
