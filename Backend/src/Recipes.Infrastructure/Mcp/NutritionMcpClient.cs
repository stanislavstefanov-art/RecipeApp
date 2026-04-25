using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Recipes.Infrastructure.Mcp;

public sealed class NutritionMcpClient : IMcpClientHost
{
    private readonly Lazy<Task<McpClient>> _clientTask;

    public NutritionMcpClient(IConfiguration configuration)
    {
        var serverProject = configuration["NutritionMcp:ServerProject"] ?? "mcp/nutrition-server";
        _clientTask = new Lazy<Task<McpClient>>(() => CreateClientAsync(serverProject));
    }

    private static Task<McpClient> CreateClientAsync(string serverProject)
    {
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Command = "dotnet",
            Arguments = ["run", "--project", serverProject, "--no-launch-profile"],
        });
        return McpClient.CreateAsync(transport);
    }

    public async Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken ct)
    {
        var client = await _clientTask.Value.WaitAsync(ct);
        var tools = await client.ListToolsAsync(cancellationToken: ct);
        return tools.Select(t => new McpToolDefinition(
            t.Name,
            t.Description ?? string.Empty,
            t.JsonSchema)).ToList();
    }

    public async Task<string> CallToolAsync(string toolName, JsonElement arguments, CancellationToken ct)
    {
        var client = await _clientTask.Value.WaitAsync(ct);

        var args = arguments.ValueKind == JsonValueKind.Null
            ? null
            : JsonSerializer.Deserialize<Dictionary<string, object?>>(arguments.GetRawText());

        var result = await client.CallToolAsync(toolName, args, cancellationToken: ct);

        if (result.IsError == true)
        {
            var errorText = result.Content
                .OfType<TextContentBlock>()
                .FirstOrDefault()?.Text ?? "Unknown MCP tool error";
            throw new InvalidOperationException($"MCP tool '{toolName}' returned error: {errorText}");
        }

        return result.Content
            .OfType<TextContentBlock>()
            .FirstOrDefault()?.Text ?? "{}";
    }

    public async ValueTask DisposeAsync()
    {
        if (_clientTask.IsValueCreated)
        {
            var client = await _clientTask.Value;
            await client.DisposeAsync();
        }
    }
}
