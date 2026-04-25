using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Recipes.McpServer.Http;

namespace Recipes.McpServer.Tools;

[McpServerToolType]
public sealed class HouseholdTools
{
    private readonly RecipesApiClient _client;

    public HouseholdTools(RecipesApiClient client) => _client = client;

    [McpServerTool(Name = "list_households"), Description("List all households (id + name).")]
    public async Task<string> ListHouseholdsAsync(CancellationToken ct)
    {
        var households = await _client.ListHouseholdsAsync(ct);
        return JsonSerializer.Serialize(households);
    }

    [McpServerTool(Name = "get_household"), Description("Get household details including members.")]
    public async Task<string> GetHouseholdAsync(
        [Description("The household ID (GUID).")] string id,
        CancellationToken ct)
    {
        var household = await _client.GetHouseholdAsync(id, ct);
        return household is null
            ? $"Household '{id}' not found."
            : JsonSerializer.Serialize(household);
    }
}
