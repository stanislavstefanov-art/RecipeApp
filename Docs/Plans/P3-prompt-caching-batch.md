# P3 — Prompt Caching and Batch API: Implementation Plan

Reference spec: `Docs/specs/P3-prompt-caching-batch.md`

Build order: batch models → application DTOs → interfaces → commands+queries →
service → DI → endpoints → CCAF doc.

---

## Step 1 — Batch model types

Create `Backend/src/Recipes.Infrastructure/AI/Claude/Models/ClaudeBatchModels.cs`:

```csharp
// cache_control support (system must be an array to attach it)
public sealed record ClaudeCacheControl(string Type = "ephemeral");
public sealed record ClaudeCachedSystemBlock(string Type, string? Text = null, ClaudeCacheControl? CacheControl = null);

// Batch submit request
public sealed record ClaudeBatchRequest(IReadOnlyList<ClaudeBatchItem> Requests);
public sealed record ClaudeBatchItem(string CustomId, ClaudeBatchItemParams Params);
public sealed record ClaudeBatchItemParams(string Model, int MaxTokens,
    IReadOnlyList<ClaudeCachedSystemBlock> System, IReadOnlyList<ClaudeMessage> Messages);

// Status poll response
public sealed record ClaudeBatchStatusResponse(string Id, string ProcessingStatus,
    ClaudeBatchRequestCounts? RequestCounts, string? ResultsUrl);
public sealed record ClaudeBatchRequestCounts(int Processing, int Succeeded,
    int Errored, int Canceled, int Expired);

// JSONL result line
public sealed record ClaudeBatchResultLine(string CustomId, ClaudeBatchItemResult Result);
public sealed record ClaudeBatchItemResult(string Type,
    ClaudeBatchResultMessage? Message, ClaudeBatchItemError? Error);
public sealed record ClaudeBatchResultMessage(
    IReadOnlyList<ClaudeResponseContentBlock> Content, ClaudeUsage? Usage);
public sealed record ClaudeBatchItemError(string Type, string Message);
```

Use `[JsonPropertyName(...)]` + `[JsonIgnore(Condition = WhenWritingNull)]` on nullable fields.

---

## Step 2 — Application DTOs

Create `Backend/src/Recipes.Application/Recipes/BatchAnalyseRecipes/BatchAnalysisDto.cs`
with `BatchSubmissionDto`, `BatchResultsDto`, `RecipeAnalysisResultDto`, `BatchCacheStatsDto`.

---

## Step 3 — Interface

Create `Backend/src/Recipes.Application/Recipes/BatchAnalyseRecipes/IRecipeBatchAnalysisService.cs`:

```csharp
public interface IRecipeBatchAnalysisService
{
    Task<BatchSubmissionDto> SubmitBatchAsync(IReadOnlyList<RecipeDto> recipes, CancellationToken ct);
    Task<BatchResultsDto>    GetResultsAsync(string batchId, CancellationToken ct);
}
```

---

## Step 4 — Submit command + handler

Create `SubmitRecipeBatchAnalysisCommand.cs`:
- Validate: count > 0, count ≤ 50
- Fetch each recipe by ID — return 404 on first miss
- Map to `RecipeDto` list
- Call `SubmitBatchAsync`

---

## Step 5 — Results query + handler

Create `GetRecipeBatchAnalysisResultsQuery.cs`:
- Validate `BatchId` not empty
- Delegate to `GetResultsAsync`

---

## Step 6 — Service implementation

Create `ClaudeRecipeBatchAnalysisService.cs`:

**Static cached system blocks** (built once, shared across all items):
```csharp
private static readonly IReadOnlyList<ClaudeCachedSystemBlock> CachedSystemBlocks =
[
    new ClaudeCachedSystemBlock(
        Type: "text",
        Text: "...quality assessment instructions, output schema...",
        CacheControl: new ClaudeCacheControl("ephemeral"))
];
```

**`SubmitBatchAsync`**:
- Build `ClaudeBatchRequest` — one `ClaudeBatchItem` per recipe
  - `custom_id = $"recipe-{recipe.Id}"`
  - `params.system = CachedSystemBlocks`
  - `params.messages = [user: recipeJson]`
- POST to `https://api.anthropic.com/v1/messages/batches`
- Headers: `x-api-key`, `anthropic-version: 2023-06-01`, `anthropic-beta: message-batches-2024-09-24`
- Return `BatchSubmissionDto`

**`GetResultsAsync`**:
- GET status: `https://api.anthropic.com/v1/messages/batches/{id}`
- If `processingStatus != "ended"` → return early with null results
- GET results: `https://api.anthropic.com/v1/messages/batches/{id}/results`
- Parse JSONL line-by-line:
  - Decode `recipeId` from `custom_id` (`"recipe-{guid}"`)
  - Accumulate `usage.InputTokens`, `OutputTokens`, `CacheCreationInputTokens`, `CacheReadInputTokens`
  - Parse `{ qualityScore, summary }` from each succeeded item's text content
- Return `BatchResultsDto` with results + accumulated `BatchCacheStatsDto`

---

## Step 7 — DI registration

```csharp
using Recipes.Application.Recipes.BatchAnalyseRecipes;
// ...
services.AddScoped<IRecipeBatchAnalysisService, ClaudeRecipeBatchAnalysisService>();
```

---

## Step 8 — Endpoints + request record

```csharp
using Recipes.Application.Recipes.BatchAnalyseRecipes;
// ...
group.MapPost("/batch-analyze", async (BatchAnalyzeRequest request, ...) =>
{
    var result = await sender.Send(new SubmitRecipeBatchAnalysisCommand(request.RecipeIds), ct);
    return result.ToHttpResult(dto => Results.Accepted($"/api/recipes/batch-analyze/{dto.BatchId}", dto));
});

group.MapGet("/batch-analyze/{batchId}", async (string batchId, ...) =>
{
    var result = await sender.Send(new GetRecipeBatchAnalysisResultsQuery(batchId), ct);
    return result.ToHttpResult(dto => Results.Ok(dto));
});

// at bottom of file:
public sealed record BatchAnalyzeRequest(IReadOnlyList<Guid> RecipeIds);
```

---

## Step 9 — CCAF doc

Create `Backend/Docs/CCAF/P3-prompt-caching-batch.md` covering:
- What this implements
- CCAF subtopics table (4.5a–d)
- Architecture diagram
- Key decisions (system-as-array, single static block, custom_id correlation, 202 Accepted, null CacheStats until ended)
