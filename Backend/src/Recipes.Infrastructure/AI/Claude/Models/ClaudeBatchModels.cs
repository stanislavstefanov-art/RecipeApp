using System.Text.Json.Serialization;

namespace Recipes.Infrastructure.AI.Claude.Models;

// ── cache_control block (used when system must be an array for cache breakpoints) ──

public sealed record ClaudeCacheControl(
    [property: JsonPropertyName("type")] string Type = "ephemeral");

public sealed record ClaudeCachedSystemBlock(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Text = null,
    [property: JsonPropertyName("cache_control"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    ClaudeCacheControl? CacheControl = null);

// ── Batch API request ──────────────────────────────────────────────────────────

public sealed record ClaudeBatchRequest(
    [property: JsonPropertyName("requests")] IReadOnlyList<ClaudeBatchItem> Requests);

public sealed record ClaudeBatchItem(
    [property: JsonPropertyName("custom_id")] string CustomId,
    [property: JsonPropertyName("params")]    ClaudeBatchItemParams Params);

public sealed record ClaudeBatchItemParams(
    [property: JsonPropertyName("model")]      string Model,
    [property: JsonPropertyName("max_tokens")] int MaxTokens,
    // system is an array of blocks (not a string) so cache_control can be applied
    [property: JsonPropertyName("system")]   IReadOnlyList<ClaudeCachedSystemBlock> System,
    [property: JsonPropertyName("messages")] IReadOnlyList<ClaudeMessage> Messages);

// ── Batch API response ─────────────────────────────────────────────────────────

public sealed record ClaudeBatchStatusResponse(
    [property: JsonPropertyName("id")]                string Id,
    [property: JsonPropertyName("processing_status")] string ProcessingStatus,
    [property: JsonPropertyName("request_counts")]    ClaudeBatchRequestCounts? RequestCounts,
    [property: JsonPropertyName("results_url")]       string? ResultsUrl);

public sealed record ClaudeBatchRequestCounts(
    [property: JsonPropertyName("processing")] int Processing,
    [property: JsonPropertyName("succeeded")]  int Succeeded,
    [property: JsonPropertyName("errored")]    int Errored,
    [property: JsonPropertyName("canceled")]   int Canceled,
    [property: JsonPropertyName("expired")]    int Expired);

// ── Batch JSONL result lines ────────────────────────────────────────────────────

public sealed record ClaudeBatchResultLine(
    [property: JsonPropertyName("custom_id")] string CustomId,
    [property: JsonPropertyName("result")]    ClaudeBatchItemResult Result);

public sealed record ClaudeBatchItemResult(
    [property: JsonPropertyName("type")]    string Type,
    [property: JsonPropertyName("message")] ClaudeBatchResultMessage? Message,
    [property: JsonPropertyName("error")]   ClaudeBatchItemError? Error);

public sealed record ClaudeBatchResultMessage(
    [property: JsonPropertyName("content")] IReadOnlyList<ClaudeResponseContentBlock> Content,
    [property: JsonPropertyName("usage")]   ClaudeUsage? Usage);

public sealed record ClaudeBatchItemError(
    [property: JsonPropertyName("type")]    string Type,
    [property: JsonPropertyName("message")] string Message);
