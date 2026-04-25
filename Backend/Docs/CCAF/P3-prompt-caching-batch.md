# P3 — Prompt Caching and Batch API

## What this implements

`POST /api/recipes/batch-analyze` accepts a list of recipe IDs and submits them to the
Anthropic Batch API (`/v1/messages/batches`) for asynchronous analysis. Each item in the
batch is analysed against the same quality-assessment system prompt; that prompt is marked
with a `cache_control: ephemeral` breakpoint so only the first item writes the cached
prefix — all subsequent items read from the cache. Returns `{ batchId, status, requestCount }`.

`GET /api/recipes/batch-analyze/{batchId}` polls the batch status. When processing has
ended it streams the JSONL results, accumulates per-item `CacheReadInputTokens` and
`CacheCreationInputTokens` into a `BatchCacheStatsDto`, and returns the complete result.

---

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **Prompt Caching — `cache_control` breakpoints (4.5a)** | `CachedSystemBlocks` is a static `IReadOnlyList<ClaudeCachedSystemBlock>` with a single block carrying `"cache_control": { "type": "ephemeral" }`. This block is re-used unchanged for every item in the batch. The first item creates the cached prefix; all subsequent items read it, reducing cost and latency |
| **Prompt Caching — cache statistics (4.5b)** | `GetResultsAsync` accumulates `CacheCreationInputTokens` and `CacheReadInputTokens` from every result item's `usage` field into `BatchCacheStatsDto`, making cache hit rate observable in the API response |
| **Batch API — submission (4.5c)** | `SubmitBatchAsync` posts a `ClaudeBatchRequest` to `POST /v1/messages/batches` with the `anthropic-beta: message-batches-2024-09-24` header. Each `ClaudeBatchItem` carries a stable `custom_id` (`recipe-{guid}`) for result correlation |
| **Batch API — result retrieval (4.5d)** | `GetResultsAsync` calls `GET /v1/messages/batches/{id}` to check `processing_status`, then streams `GET /v1/messages/batches/{id}/results` and parses the JSONL response line-by-line |

---

## Architecture

```
POST /api/recipes/batch-analyze
        │  SubmitRecipeBatchAnalysisHandler
        │  ├── fetch all requested recipes from DB
        │  └── IRecipeBatchAnalysisService.SubmitBatchAsync(recipes)
        │
        └── ClaudeRecipeBatchAnalysisService.SubmitBatchAsync
             ├── build ClaudeBatchRequest: one item per recipe
             │    each item: system = [CachedSystemBlocks], messages = [user: recipeJson]
             └── POST /v1/messages/batches → BatchSubmissionDto { batchId, status, requestCount }
             Response: 202 Accepted with Location: /api/recipes/batch-analyze/{batchId}

GET /api/recipes/batch-analyze/{batchId}
        │  GetRecipeBatchAnalysisResultsHandler
        └── IRecipeBatchAnalysisService.GetResultsAsync(batchId)
             ├── GET /v1/messages/batches/{id}  → processing_status
             │    if still processing → return BatchResultsDto { status: "in_progress", results: null }
             └── GET /v1/messages/batches/{id}/results  → JSONL stream
                  parse each line → RecipeAnalysisResultDto { qualityScore, summary }
                  accumulate usage → BatchCacheStatsDto
                  return BatchResultsDto { status: "ended", results, cacheStats }
```

---

## Key decisions

### `system` as an array of blocks, not a string

The simple `/v1/messages` API accepts `system` as a plain string. To attach
`cache_control`, the system field must be an array of typed content blocks. A new
`ClaudeCachedSystemBlock` record is introduced alongside a `ClaudeCacheControl` record —
the existing `ClaudeMessagesRequest` is left unchanged to avoid breaking existing features.
`ClaudeBatchItemParams` owns the array-typed system field.

### Single static breakpoint covers the entire system prompt

The quality-assessment instructions (scoring guide, output schema, rules) are static
across all batch items. A single `cache_control: ephemeral` on the one system block
caches everything up to and including it. All items after the first in each batch read
the same cached prefix — the first item's `CacheCreationInputTokens` will be non-zero;
all subsequent items will show `CacheReadInputTokens` instead.

### Batch items use `custom_id: "recipe-{guid}"` for result correlation

The Batch API returns results in arbitrary order. Embedding the recipe GUID in the
`custom_id` lets `GetResultsAsync` decode the ID from each JSONL line without keeping an
additional in-memory lookup table.

### Submit returns 202 Accepted, not 200 OK

`POST /api/recipes/batch-analyze` returns HTTP 202 with a `Location` header pointing to
the status endpoint. This matches REST semantics for async operations and signals to
callers that they must poll for results.

### `CacheStatsDto` is null until the batch has ended

The `BatchResultsDto.CacheStats` field is null while processing is still in progress.
Once the batch ends, cache statistics are accumulated from per-item `usage` objects and
exposed in `CacheStatsDto`. A non-zero `CacheReadTokens` : `TotalInputTokens` ratio
confirms that caching is working.
