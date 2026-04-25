using System.Text.Json;
using System.Text.Json.Serialization;

namespace Recipes.Infrastructure.AI.Claude.Models;

// ── Shared content block (request + response) ─────────────────────────────
// One record covers all variants: text, tool_use (from Claude), tool_result (from us).
// Fields unused by a given variant are null and omitted from JSON.

public sealed record ClaudeAgentContentBlock(
    [property: JsonPropertyName("type")]        string       Type,
    [property: JsonPropertyName("text"),        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string?      Text      = null,
    [property: JsonPropertyName("id"),          JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string?      Id        = null,
    [property: JsonPropertyName("name"),        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string?      Name      = null,
    [property: JsonPropertyName("input"),       JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] JsonElement? Input     = null,
    [property: JsonPropertyName("tool_use_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string?      ToolUseId = null,
    [property: JsonPropertyName("content"),     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string?      Content   = null,
    [property: JsonPropertyName("is_error"),    JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool?        IsError   = null
);

// ── Request ────────────────────────────────────────────────────────────────

public sealed record ClaudeAgentMessage(
    [property: JsonPropertyName("role")]    string                             Role,
    [property: JsonPropertyName("content")] IReadOnlyList<ClaudeAgentContentBlock> Content
);

public sealed record ClaudeToolDefinition(
    [property: JsonPropertyName("name")]         string      Name,
    [property: JsonPropertyName("description")]  string      Description,
    [property: JsonPropertyName("input_schema")] JsonElement InputSchema
);

public sealed record ClaudeToolChoiceAuto(
    [property: JsonPropertyName("type")] string Type = "auto"
);

public sealed record ClaudeAgentRequest(
    [property: JsonPropertyName("model")]       string                             Model,
    [property: JsonPropertyName("max_tokens")]  int                                MaxTokens,
    [property: JsonPropertyName("system")]      string                             System,
    [property: JsonPropertyName("tools")]       IReadOnlyList<ClaudeToolDefinition> Tools,
    [property: JsonPropertyName("tool_choice")] object                             ToolChoice,
    [property: JsonPropertyName("messages")]    IReadOnlyList<ClaudeAgentMessage>  Messages
);

// ── Response ───────────────────────────────────────────────────────────────

public sealed record ClaudeAgentResponse(
    [property: JsonPropertyName("id")]          string                             Id,
    [property: JsonPropertyName("stop_reason")] string                             StopReason,
    [property: JsonPropertyName("content")]     IReadOnlyList<ClaudeAgentContentBlock> Content,
    [property: JsonPropertyName("usage")]       ClaudeUsage?                       Usage
);

public sealed record ClaudeUsage(
    [property: JsonPropertyName("input_tokens")]                int  InputTokens,
    [property: JsonPropertyName("output_tokens")]               int  OutputTokens,
    [property: JsonPropertyName("cache_read_input_tokens")]     int? CacheReadInputTokens,
    [property: JsonPropertyName("cache_creation_input_tokens")] int? CacheCreationInputTokens
);
