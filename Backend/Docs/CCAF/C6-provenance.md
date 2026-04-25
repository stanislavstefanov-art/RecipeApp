# C6 — AI Output Provenance

## What this implements

Attaches a provenance record to every successful AI-generated output produced by the
new recipe services (critique, scaling, jury review, batch analysis). Each record
captures `(feature, model, promptVersion, generatedAt)`, where `promptVersion` is
the first 8 hex characters of the SHA-256 of the system prompt text — changing when
the prompt changes, stable when it doesn't. The `ProvenanceId` is embedded in each
affected response DTO, creating a visible link between the API response and its audit
record. `GET /api/admin/provenance` returns recent records newest-first.

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **5.6 — Provenance** | Each new AI service (`ClaudeRecipeCritiqueService`, `ClaudeRecipeScalingService`, `ClaudeRecipeDraftReviewService`, `ClaudeRecipeBatchAnalysisService`) injects `IProvenanceStore` and calls `Record(feature, model, promptVersion)` on each successful inference; the returned `ProvenanceId` is embedded in the response DTO (`RecipeCritiqueDto`, `ScaledRecipeDto`, `RecipeDraftReviewDto`, `BatchSubmissionDto`); `GET /api/admin/provenance` surfaces the full audit log — establishing a chain-of-custody from API response back to the exact model and prompt version that produced it |

## Architecture

```
POST /api/recipes/{id}/critique
        │  ClaudeRecipeCritiqueService.CritiqueAsync()
        │  ├── POST /v1/messages → Claude
        │  ├── IProvenanceStore.Record("recipe-critique", model, promptHash)
        │  │     → ProvenanceId
        │  └── RecipeCritiqueDto { ..., ProvenanceId }
        └── 200 OK

POST /api/recipes/{id}/scale
        │  ClaudeRecipeScalingService.ScaleAsync()
        │  ├── multi-turn retry → Claude
        │  ├── IProvenanceStore.Record("recipe-scaling", model, promptHash)
        │  └── ScaledRecipeDto { ..., ProvenanceId }
        └── 200 OK

POST /api/recipes/import/with-jury
        │  ClaudeRecipeDraftReviewService.ReviewAsync()
        │  ├── Task.WhenAll([3 jurors])
        │  ├── IProvenanceStore.Record("recipe-jury", model, combined-promptHash)
        │  └── RecipeDraftReviewDto { ..., ProvenanceId }
        └── 200 OK

POST /api/recipes/batch-analyze
        │  ClaudeRecipeBatchAnalysisService.SubmitBatchAsync()
        │  ├── POST /v1/messages/batches
        │  ├── IProvenanceStore.Record("recipe-batch", model, promptHash)
        │  └── BatchSubmissionDto { ..., ProvenanceId }
        └── 202 Accepted

GET /api/admin/provenance?limit=N
        │  GetProvenanceHandler
        │  └── IProvenanceStore.GetRecent(limit)
        └── ProvenanceRecord[] (newest first)
```

## Key decisions

**SHA-256 of system prompt as version** — computing an 8-character hex prefix of the
system prompt's hash ties provenance to the exact prompt text, not an arbitrary
version string. If the prompt changes, the hash changes automatically; no manual
version bumping required.

**Combined hash for jury service** — `ClaudeRecipeDraftReviewService` uses three
system prompts (one per persona). The prompt version is computed from their
concatenation, so any change to any persona changes the recorded version.

**`ProvenanceId` in response DTO** — embedding the ID in the API response means
callers can immediately reference the provenance record without an extra lookup.
This is the minimal footprint to make the link visible.

**Pre-existing services untouched** — `RecipeImport`, `MealPlanSuggestion`,
`IngredientSubstitution`, and `ExpenseInsight` are not modified per the project
constraint that existing AI implementations are preserved as-is.

**Singleton `ConcurrentQueue` store** — `InMemoryProvenanceStore` uses
`ConcurrentQueue<ProvenanceRecord>` for lock-free append and `TakeLast` for
efficient newest-first retrieval without sorting.
