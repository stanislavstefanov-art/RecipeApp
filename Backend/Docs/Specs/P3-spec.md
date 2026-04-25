# P3 Spec — Prompt Caching and Batch API (Batch Recipe Analysis)

## Goal

Add a batch recipe analysis feature that submits multiple recipes to Anthropic's Batch
API (`/v1/messages/batches`) and marks the shared system prompt with a `cache_control`
breakpoint. Because all batch items use the same static analysis prompt, only the first
item writes the cached prefix — all subsequent items read from it. Cache statistics
(`CacheCreationInputTokens`, `CacheReadInputTokens`) are surfaced in the results response.

## CCAF subtopics targeted

| Subtopic | How covered |
|---|---|
| **4.5a — `cache_control` breakpoints** | System prompt sent as an array of `ClaudeCachedSystemBlock` with `cache_control: ephemeral` on the single shared prompt block |
| **4.5b — Cache statistics** | `GetResultsAsync` accumulates per-item token usage into `BatchCacheStatsDto { CacheCreationTokens, CacheReadTokens }` |
| **4.5c — Batch API submission** | `SubmitBatchAsync` POSTs `ClaudeBatchRequest` to `/v1/messages/batches` with `anthropic-beta: message-batches-2024-09-24` |
| **4.5d — Batch API result retrieval** | `GetResultsAsync` calls `GET /v1/messages/batches/{id}` then `GET /v1/messages/batches/{id}/results` (JSONL stream) |

## Proposed API

```
POST /api/recipes/batch-analyze
Body: { "recipeIds": ["guid", ...] }  (1–50 IDs)
→ 202 Accepted  +  Location: /api/recipes/batch-analyze/{batchId}
   { "batchId": "msgbatch_...", "status": "in_progress", "requestCount": 5 }

GET /api/recipes/batch-analyze/{batchId}
→ 200
   {
     "batchId": "...",
     "processingStatus": "in_progress" | "ended",
     "results": null | [ { recipeId, succeeded, qualityScore, summary } ],
     "cacheStats": null | { totalInputTokens, totalOutputTokens,
                            cacheCreationTokens, cacheReadTokens }
   }
```

## System prompt + cache_control design

```
system: [
  {
    "type": "text",
    "text": "...quality assessment instructions...",
    "cache_control": { "type": "ephemeral" }   ← breakpoint here
  }
]
```

This is a single-block system. The breakpoint covers the entire instruction block.
All batch items share this identical block, so Anthropic caches it after the first item.

## Key design decisions

- **`system` as an array, not a string** — `cache_control` requires the system field to
  be an array of typed content blocks. A new `ClaudeCachedSystemBlock` record is added to
  Infrastructure models; the existing `ClaudeMessagesRequest.System: string` is unchanged.
- **`ClaudeBatchModels.cs` — new model file** — keeps batch-specific types (request, status
  response, JSONL result lines) isolated from the agentic models in `ClaudeToolModels.cs`.
- **`custom_id: "recipe-{guid}"`** — embeds the recipe ID in the batch item identifier for
  correlation at result parse time without a separate lookup map.
- **`POST` returns 202 Accepted** — async operation; REST semantics require 202 + Location.
  The GET endpoint is polled by the client.
- **`CacheStats` is null until ended** — the stats are only meaningful once all result items
  are available. Returning null while processing avoids misleading partial totals.

## File plan

### Create
- `Backend/src/Recipes.Infrastructure/AI/Claude/Models/ClaudeBatchModels.cs`
- `Backend/src/Recipes.Application/Recipes/BatchAnalyseRecipes/BatchAnalysisDto.cs`
- `Backend/src/Recipes.Application/Recipes/BatchAnalyseRecipes/IRecipeBatchAnalysisService.cs`
- `Backend/src/Recipes.Application/Recipes/BatchAnalyseRecipes/SubmitRecipeBatchAnalysisCommand.cs` (command + handler)
- `Backend/src/Recipes.Application/Recipes/BatchAnalyseRecipes/GetRecipeBatchAnalysisResultsQuery.cs` (query + handler)
- `Backend/src/Recipes.Infrastructure/AI/Claude/Services/ClaudeRecipeBatchAnalysisService.cs`
- `Backend/Docs/CCAF/P3-prompt-caching-batch.md`

### Modify
- `Backend/src/Recipes.Infrastructure/DependencyInjection.cs` — register `IRecipeBatchAnalysisService`
- `Backend/src/Recipes.Api/Endpoints/RecipesEndpoints.cs` — add `POST /batch-analyze`, `GET /batch-analyze/{batchId}`, `BatchAnalyzeRequest` record
