# C6 — AI Output Provenance

## Summary

Attaches a provenance record to every successful AI-generated output produced by the
new application services (recipe critique, recipe scaling, jury review, batch
analysis). Each record captures `(feature, model, promptVersion, generatedAt)`,
where `promptVersion` is the first 8 hex characters of the SHA-256 of the system
prompt — allowing audit of exactly which prompt produced which output even as prompts
evolve. Each affected response DTO gains a `ProvenanceId` field that callers can use
to look up the full provenance record. A new `GET /api/admin/provenance` endpoint
lists recent records.

No existing endpoints are modified in their request shape. The four pre-existing
Claude services (`RecipeImport`, `MealPlanSuggestion`, `IngredientSubstitution`,
`ExpenseInsight`) are not touched.

---

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **5.6 — Provenance** | Every new AI service injects `IProvenanceStore` and calls `Record(feature, model, promptVersion)` on each successful inference; the returned `ProvenanceId` is embedded in the response DTO; `GET /api/admin/provenance` surfaces the full audit log — creating a complete chain-of-custody for AI-generated content |

---

## Architecture

```
POST /api/recipes/{id}/critique
        │  CritiqueRecipeHandler
        │  └── IRecipeCritiqueService.CritiqueAsync(dto)
        │        └── ClaudeRecipeCritiqueService
        │              ├── POST /v1/messages → Claude
        │              ├── IProvenanceStore.Record("recipe-critique", model, promptHash)
        │              │     → ProvenanceId (Guid)
        │              └── RecipeCritiqueDto { ..., ProvenanceId }
        └── 200 OK

POST /api/recipes/{id}/scale
        │  ScaleRecipeHandler
        │  └── IRecipeScalingService.ScaleAsync(dto, from, to)
        │        └── ClaudeRecipeScalingService
        │              ├── multi-turn retry loop → Claude
        │              ├── IProvenanceStore.Record("recipe-scaling", model, promptHash)
        │              └── ScaledRecipeDto { ..., ProvenanceId }
        └── 200 OK

POST /api/recipes/import/with-jury
        │  ReviewRecipeDraftHandler
        │  └── IRecipeDraftReviewService.ReviewAsync(draft)
        │        └── ClaudeRecipeDraftReviewService
        │              ├── Task.WhenAll([3 jurors])
        │              ├── IProvenanceStore.Record("recipe-jury", model, promptHash)
        │              └── RecipeDraftReviewDto { ..., ProvenanceId }
        └── 200 OK

POST /api/recipes/batch-analyze
        │  SubmitRecipeBatchAnalysisHandler
        │  └── IRecipeBatchAnalysisService.SubmitBatchAsync(recipes)
        │        └── ClaudeRecipeBatchAnalysisService
        │              ├── POST /v1/messages/batches → Anthropic
        │              ├── IProvenanceStore.Record("recipe-batch", model, promptHash)
        │              └── BatchSubmissionDto { ..., ProvenanceId }
        └── 202 Accepted

GET /api/admin/provenance?limit=N
        │  GetProvenanceQuery
        │  └── IProvenanceStore.GetRecent(limit)
        └── IReadOnlyList<ProvenanceRecord>
```

---

## ProvenanceRecord and store

```csharp
public sealed record ProvenanceRecord(
    Guid Id,
    string Feature,         // "recipe-critique" | "recipe-scaling" | "recipe-jury" | "recipe-batch"
    string Model,           // e.g. "claude-haiku-4-5"
    string PromptVersion,   // first 8 hex chars of SHA-256 of system prompt
    DateTime GeneratedAt);

public interface IProvenanceStore
{
    Guid Record(string feature, string model, string promptVersion);
    IReadOnlyList<ProvenanceRecord> GetRecent(int limit);
}
```

---

## Prompt version derivation

Each service computes `_promptVersion` once at construction time:

```csharp
private readonly string _promptVersion;

// In constructor, after building _systemPrompt:
using var sha = SHA256.Create();
var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(_systemPrompt));
_promptVersion = Convert.ToHexString(hash)[..8].ToLowerInvariant();
```

If the system prompt text changes (e.g., prompt engineering iteration), the hash
changes and new records carry the new version — providing a meaningful audit trail.

---

## DTO changes (additive)

```csharp
// RecipeCritiqueDto gains:
Guid ProvenanceId

// ScaledRecipeDto gains:
Guid ProvenanceId

// RecipeDraftReviewDto gains:
Guid ProvenanceId

// BatchSubmissionDto gains:
Guid ProvenanceId
```

All additions are positional record fields appended at the end — existing
deserialization of prior responses is unaffected because the field is new.

---

## Files to create

| Path | Purpose |
|---|---|
| `Backend/src/Recipes.Application/Common/AI/IProvenanceStore.cs` | Store interface + `ProvenanceRecord` |
| `Backend/src/Recipes.Application/Admin/GetProvenance/GetProvenanceQuery.cs` | Query + handler |
| `Backend/src/Recipes.Infrastructure/AI/Provenance/InMemoryProvenanceStore.cs` | Singleton store implementation |
| `Backend/Docs/CCAF/C6-provenance.md` | CCAF documentation |

## Files to modify

| Path | Change |
|---|---|
| `Backend/src/Recipes.Application/Recipes/CritiqueRecipe/RecipeCritiqueDto.cs` | Add `Guid ProvenanceId` |
| `Backend/src/Recipes.Application/Recipes/ScaleRecipe/ScaledRecipeDto.cs` | Add `Guid ProvenanceId` |
| `Backend/src/Recipes.Application/Recipes/ReviewRecipeDraft/RecipeDraftReviewDto.cs` | Add `Guid ProvenanceId` |
| `Backend/src/Recipes.Application/Recipes/BatchAnalyseRecipes/BatchAnalysisDto.cs` | Add `Guid ProvenanceId` to `BatchSubmissionDto` |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Services/ClaudeRecipeCritiqueService.cs` | Inject store, compute hash, record, embed ID |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Services/ClaudeRecipeScalingService.cs` | Inject store, compute hash, record, embed ID |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Services/ClaudeRecipeDraftReviewService.cs` | Inject store, compute hash, record, embed ID |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Services/ClaudeRecipeBatchAnalysisService.cs` | Inject store, compute hash, record, embed ID |
| `Backend/src/Recipes.Infrastructure/DependencyInjection.cs` | Register `IProvenanceStore` (singleton) |
| `Backend/src/Recipes.Api/Endpoints/AdminEndpoints.cs` | Add `GET /api/admin/provenance` |

---

## Acceptance criteria

1. `dotnet build Backend/Recipes.sln` passes.
2. `RecipeCritiqueDto`, `ScaledRecipeDto`, `RecipeDraftReviewDto`, and `BatchSubmissionDto`
   responses each include a non-empty `provenanceId` field.
3. The `provenanceId` values are consistent — the same ID returned in a response appears in
   `GET /api/admin/provenance`.
4. `GET /api/admin/provenance` returns records with correct `feature`, `model`, and an
   8-character `promptVersion` hex string.
5. Changing the system prompt text (any character) produces a different `promptVersion` in
   subsequent records.
6. Existing tests in `Recipes.Application.Tests` still pass (51 tests).
