using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;
using Recipes.McpServer.Http;

namespace Recipes.McpServer.Resources;

[McpServerResourceType]
public sealed class RecipeAppResources
{
    private readonly RecipesApiClient _client;

    public RecipeAppResources(RecipesApiClient client) => _client = client;

    [McpServerResource(UriTemplate = "recipe://{id}", Name = "Recipe", MimeType = "text/plain")]
    [Description("Recipe name, ingredient list, and numbered steps.")]
    public async Task<string> GetRecipeAsync(string id, CancellationToken ct)
    {
        var recipe = await _client.GetRecipeAsync(id, ct);
        if (recipe is null) return $"Recipe '{id}' not found.";

        var sb = new StringBuilder();
        sb.AppendLine($"# {recipe.Name}");
        sb.AppendLine();
        sb.AppendLine("## Ingredients");
        foreach (var ing in recipe.Ingredients)
            sb.AppendLine($"- {ing.Quantity} {ing.Unit} {ing.Name}");
        sb.AppendLine();
        sb.AppendLine("## Steps");
        foreach (var step in recipe.Steps.OrderBy(s => s.Order))
            sb.AppendLine($"{step.Order}. {step.Instruction}");
        return sb.ToString();
    }

    [McpServerResource(UriTemplate = "meal-plan://{id}", Name = "MealPlan", MimeType = "text/plain")]
    [Description("Week-view table: date × meal type, recipe name, assignees.")]
    public async Task<string> GetMealPlanAsync(string id, CancellationToken ct)
    {
        var plan = await _client.GetMealPlanAsync(id, ct);
        if (plan is null) return $"Meal plan '{id}' not found.";

        var sb = new StringBuilder();
        sb.AppendLine($"# {plan.Name}  ({plan.HouseholdName})");
        sb.AppendLine();
        sb.AppendLine("| Date       | Meal      | Recipe              | Assigned to         |");
        sb.AppendLine("|------------|-----------|---------------------|---------------------|");
        foreach (var entry in plan.Entries.OrderBy(e => e.PlannedDate))
        {
            var mealType = entry.MealType switch
            {
                0 => "Breakfast",
                1 => "Lunch",
                2 => "Dinner",
                _ => $"Meal {entry.MealType}"
            };
            var assignees = entry.Assignments.Count == 0
                ? "—"
                : string.Join(", ", entry.Assignments.Select(a => $"{a.PersonName} (×{a.PortionMultiplier})"));
            sb.AppendLine($"| {entry.PlannedDate:yyyy-MM-dd} | {mealType,-9} | {entry.BaseRecipeName,-19} | {assignees,-19} |");
        }
        return sb.ToString();
    }

    [McpServerResource(UriTemplate = "shopping-list://{id}", Name = "ShoppingList", MimeType = "text/plain")]
    [Description("Grouped list: pending items / purchased items, quantities.")]
    public async Task<string> GetShoppingListAsync(string id, CancellationToken ct)
    {
        var list = await _client.GetShoppingListAsync(id, ct);
        if (list is null) return $"Shopping list '{id}' not found.";

        var sb = new StringBuilder();
        sb.AppendLine($"# {list.Name}");
        sb.AppendLine();
        sb.AppendLine("## Pending");
        foreach (var item in list.Items.Where(i => !i.IsPurchased))
            sb.AppendLine($"- [ ] {item.Quantity} {item.Unit} {item.ProductName}");
        sb.AppendLine();
        sb.AppendLine("## Purchased");
        foreach (var item in list.Items.Where(i => i.IsPurchased))
            sb.AppendLine($"- [x] {item.Quantity} {item.Unit} {item.ProductName}");
        return sb.ToString();
    }
}
