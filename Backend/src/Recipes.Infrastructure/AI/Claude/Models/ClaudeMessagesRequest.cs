using System.Text.Json.Serialization;

namespace Recipes.Infrastructure.AI.Claude.Models;

public sealed record ClaudeMessagesRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("max_tokens")] int MaxTokens,
    [property: JsonPropertyName("system")] string System,
    [property: JsonPropertyName("messages")] IReadOnlyList<ClaudeMessage> Messages);

public sealed record ClaudeMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] IReadOnlyList<ClaudeContentBlock> Content);

public sealed record ClaudeContentBlock(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] string Text);