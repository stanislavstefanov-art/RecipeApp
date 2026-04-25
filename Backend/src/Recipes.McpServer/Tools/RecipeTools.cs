using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Recipes.McpServer.Http;

namespace Recipes.McpServer.Tools;

[McpServerToolType]
public sealed class RecipeTools
{
    private readonly RecipesApiClient _client;

    public RecipeTools(RecipesApiClient client) => _client = client;

    [McpServerTool(Name = "list_recipes"), Description("List all recipes (id + name).")]
    public async Task<string> ListRecipesAsync(CancellationToken ct)
    {
        var recipes = await _client.ListRecipesAsync(ct);
        return JsonSerializer.Serialize(recipes);
    }

    [McpServerTool(Name = "get_recipe"), Description("Get full recipe details including ingredients and steps.")]
    public async Task<string> GetRecipeAsync(
        [Description("The recipe ID (GUID).")] string id,
        CancellationToken ct)
    {
        var recipe = await _client.GetRecipeAsync(id, ct);
        return recipe is null
            ? $"Recipe '{id}' not found."
            : JsonSerializer.Serialize(recipe);
    }

    [McpServerTool(Name = "search_recipes_by_ingredient"), Description("Search recipes that contain the given ingredient.")]
    public async Task<string> SearchRecipesByIngredientAsync(
        [Description("Ingredient name to search for.")] string ingredient,
        CancellationToken ct)
    {
        var recipes = await _client.SearchRecipesByIngredientAsync(ingredient, ct);
        return JsonSerializer.Serialize(recipes);
    }
}
