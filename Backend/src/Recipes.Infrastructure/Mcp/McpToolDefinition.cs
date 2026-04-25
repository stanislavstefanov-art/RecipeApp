using System.Text.Json;

namespace Recipes.Infrastructure.Mcp;

public sealed record McpToolDefinition(
    string Name,
    string Description,
    JsonElement InputSchema);
