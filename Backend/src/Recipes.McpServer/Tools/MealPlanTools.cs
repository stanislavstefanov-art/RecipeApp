using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Recipes.McpServer.Http;

namespace Recipes.McpServer.Tools;

[McpServerToolType]
public sealed class MealPlanTools
{
    private readonly RecipesApiClient _client;

    public MealPlanTools(RecipesApiClient client) => _client = client;

    [McpServerTool(Name = "list_meal_plans"), Description("List all meal plans (id + name).")]
    public async Task<string> ListMealPlansAsync(CancellationToken ct)
    {
        var plans = await _client.ListMealPlansAsync(ct);
        return JsonSerializer.Serialize(plans);
    }

    [McpServerTool(Name = "get_meal_plan"), Description("Get full meal plan details: entries, assignments, dates.")]
    public async Task<string> GetMealPlanAsync(
        [Description("The meal plan ID (GUID).")] string id,
        CancellationToken ct)
    {
        var plan = await _client.GetMealPlanAsync(id, ct);
        return plan is null
            ? $"Meal plan '{id}' not found."
            : JsonSerializer.Serialize(plan);
    }
}
