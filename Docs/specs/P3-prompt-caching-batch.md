# P3 — Prompt Caching and Batch API (Batch Recipe Analysis)

## Summary

Adds batch recipe analysis using the Anthropic Batch API (`/v1/messages/batches`).
The shared quality-assessment system prompt is marked with `cache_control: ephemeral`
so only the first batch item writes the cached prefix — all subsequent items read it.
Two endpoints: `POST /api/recipes/batch-analyze` submits the batch (async, 202 Accepted),
`GET /api/recipes/batch-analyze/{batchId}` polls status and returns parsed JSONL results
with accumulated cache statistics.

No existing endpoints are modified.

---

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **4.5a — `cache_control` breakpoints** | System prompt sent as `IReadOnlyList<ClaudeCachedSystemBlock>` with `cache_control: { type: "ephemeral" }` on the single shared block |
| **4.5b — Cache statistics** | `GetResultsAsync` accumulates `CacheCreationInputTokens` / `CacheReadInputTokens` per item into `BatchCacheStatsDto` |
| **4.5c — Batch API submission** | `SubmitBatchAsync` POSTs `ClaudeBatchRequest` to `POST /v1/messages/batches` with `anthropic-beta: message-batches-2024-09-24` |
| **4.5d — Batch API result retrieval** | `GetResultsAsync` calls `GET /v1/messages/batches/{id}` (status), then `GET /v1/messages/batches/{id}/results` (JSONL stream) |

---

## Architecture

```
POST /api/recipes/batch-analyze  { recipeIds: [...] }
        │  SubmitRecipeBatchAnalysisHandler
        │  ├── fetch each recipe from DB
        │  └── IRecipeBatchAnalysisService.SubmitBatchAsync(recipes)
        └── → 202 Accepted  +  Location: /api/recipes/batch-analyze/{batchId}

GET /api/recipes/batch-analyze/{batchId}
        │  GetRecipeBatchAnalysisResultsHandler
        └── IRecipeBatchAnalysisService.GetResultsAsync(batchId)
             ├── GET /v1/messages/batches/{id}  → processingStatus
             │   if "in_progress" → BatchResultsDto { status, results: null, cacheStats: null }
             └── GET /v1/messages/batches/{id}/results  → JSONL
                  parse lines → RecipeAnalysisResultDto[]
                  accumulate usage → BatchCacheStatsDto
                  return BatchResultsDto { status: "ended", results, cacheStats }
```

---

## DTOs

```csharp
record BatchSubmissionDto(string BatchId, string Status, int RequestCount);

record BatchResultsDto(
    string BatchId,
    string ProcessingStatus,
    IReadOnlyList<RecipeAnalysisResultDto>? Results,  // null while processing
    BatchCacheStatsDto? CacheStats);                  // null while processing

record RecipeAnalysisResultDto(
    Guid RecipeId, string RecipeName, bool Succeeded,
    int? QualityScore, string? Summary, string? ErrorMessage);

record BatchCacheStatsDto(
    int TotalInputTokens, int TotalOutputTokens,
    int CacheCreationTokens, int CacheReadTokens);
```

---

## `cache_control` placement

```
system: [
  {
    "type": "text",
    "text": "...quality assessment instructions...",
    "cache_control": { "type": "ephemeral" }
  }
]
```

Single block covers the entire system prompt. All batch items share this identical block.
First item: `CacheCreationInputTokens > 0`. Subsequent items: `CacheReadInputTokens > 0`.

---

## New model types required

`ClaudeBatchModels.cs` in `Infrastructure/AI/Claude/Models/`:
- `ClaudeCacheControl`, `ClaudeCachedSystemBlock` — for `cache_control` on system blocks
- `ClaudeBatchRequest`, `ClaudeBatchItem`, `ClaudeBatchItemParams` — batch submit request
- `ClaudeBatchStatusResponse`, `ClaudeBatchRequestCounts` — status poll response
- `ClaudeBatchResultLine`, `ClaudeBatchItemResult`, `ClaudeBatchResultMessage`, `ClaudeBatchItemError` — JSONL parsing

Existing `ClaudeMessagesRequest` (string `System`) is left unchanged.

---

## Files to create

| Path | Purpose |
|---|---|
| `Backend/src/Recipes.Infrastructure/AI/Claude/Models/ClaudeBatchModels.cs` | Batch API + cache_control model types |
| `Backend/src/Recipes.Application/Recipes/BatchAnalyseRecipes/BatchAnalysisDto.cs` | Response DTOs |
| `Backend/src/Recipes.Application/Recipes/BatchAnalyseRecipes/IRecipeBatchAnalysisService.cs` | Service interface |
| `Backend/src/Recipes.Application/Recipes/BatchAnalyseRecipes/SubmitRecipeBatchAnalysisCommand.cs` | Submit command + handler |
| `Backend/src/Recipes.Application/Recipes/BatchAnalyseRecipes/GetRecipeBatchAnalysisResultsQuery.cs` | Results query + handler |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Services/ClaudeRecipeBatchAnalysisService.cs` | Service implementation |
| `Backend/Docs/CCAF/P3-prompt-caching-batch.md` | CCAF documentation |

## Files to modify

| Path | Change |
|---|---|
| `Backend/src/Recipes.Infrastructure/DependencyInjection.cs` | Register `IRecipeBatchAnalysisService` |
| `Backend/src/Recipes.Api/Endpoints/RecipesEndpoints.cs` | Add two batch endpoints + `BatchAnalyzeRequest` record |

---

## Acceptance criteria

1. `dotnet build Backend/Recipes.sln` passes.
2. `POST /api/recipes/batch-analyze` with empty `recipeIds` returns 400.
3. `POST /api/recipes/batch-analyze` with unknown recipe ID returns 404.
4. A valid submit returns 202 with a non-empty `batchId`.
5. `GET /api/recipes/batch-analyze/{batchId}` while processing returns `processingStatus: "in_progress"`, `results: null`.
6. When processing ends, `cacheStats.cacheReadTokens > 0` for batches with more than one item (confirming the cache was hit).
