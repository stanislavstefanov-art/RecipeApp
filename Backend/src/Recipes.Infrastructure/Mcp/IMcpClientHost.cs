using System.Text.Json;

namespace Recipes.Infrastructure.Mcp;

public interface IMcpClientHost : IAsyncDisposable
{
    Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken ct);
    Task<string> CallToolAsync(string toolName, JsonElement arguments, CancellationToken ct);
}
