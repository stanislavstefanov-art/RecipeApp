using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Recipes.McpServer.Http;

namespace Recipes.McpServer.Tools;

[McpServerToolType]
public sealed class ShoppingListTools
{
    private readonly RecipesApiClient _client;

    public ShoppingListTools(RecipesApiClient client) => _client = client;

    [McpServerTool(Name = "list_shopping_lists"), Description("List all shopping lists (id + name).")]
    public async Task<string> ListShoppingListsAsync(CancellationToken ct)
    {
        var lists = await _client.ListShoppingListsAsync(ct);
        return JsonSerializer.Serialize(lists);
    }

    [McpServerTool(Name = "get_shopping_list"), Description("Get full shopping list details: items, quantities, purchased status.")]
    public async Task<string> GetShoppingListAsync(
        [Description("The shopping list ID (GUID).")] string id,
        CancellationToken ct)
    {
        var list = await _client.GetShoppingListAsync(id, ct);
        return list is null
            ? $"Shopping list '{id}' not found."
            : JsonSerializer.Serialize(list);
    }

    [McpServerTool(Name = "mark_item_pending"), Description("Mark a purchased shopping list item back to pending.")]
    public async Task<string> MarkItemPendingAsync(
        [Description("The shopping list ID (GUID).")] string shoppingListId,
        [Description("The item ID (GUID).")] string itemId,
        CancellationToken ct)
    {
        await _client.MarkItemPendingAsync(shoppingListId, itemId, ct);
        return "Item marked as pending.";
    }

    [McpServerTool(Name = "mark_item_purchased"), Description("Mark a shopping list item as purchased.")]
    public async Task<string> MarkItemPurchasedAsync(
        [Description("The shopping list ID (GUID).")] string shoppingListId,
        [Description("The item ID (GUID).")] string itemId,
        CancellationToken ct)
    {
        await _client.MarkItemPurchasedAsync(shoppingListId, itemId, ct);
        return "Item marked as purchased.";
    }
}
