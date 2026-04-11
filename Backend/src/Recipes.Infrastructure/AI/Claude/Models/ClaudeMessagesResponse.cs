using System.Text.Json.Serialization;

namespace Recipes.Infrastructure.AI.Claude.Models;

public sealed record ClaudeMessagesResponse(
    [property: JsonPropertyName("content")] IReadOnlyList<ClaudeResponseContentBlock> Content);

public sealed record ClaudeResponseContentBlock(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] string? Text);