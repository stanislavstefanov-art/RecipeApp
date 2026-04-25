# F4 — MCP Server (stdio): Implementation Plan

Reference spec: `Docs/Specs/F4-mcp-server-stdio.md`

Build order: scaffold → DTOs → HTTP client → host → tools → resources → prompts → wire-up → registration files → docs.

---

## Step 1 — Scaffold project

Run from repo root:

```
dotnet new console -n Recipes.McpServer -o Backend/src/Recipes.McpServer --framework net10.0
dotnet add Backend/src/Recipes.McpServer package ModelContextProtocol
dotnet add Backend/src/Recipes.McpServer package Microsoft.Extensions.Hosting
dotnet add Backend/src/Recipes.McpServer package Microsoft.Extensions.Http
dotnet sln Backend/Recipes.sln add Backend/src/Recipes.McpServer/Recipes.McpServer.csproj
```

**Final `.csproj`:**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>Recipes.McpServer</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="ModelContextProtocol" Version="*-*" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.*" />
    <PackageReference Include="Microsoft.Extensions.Http" Version="10.0.*" />
  </ItemGroup>
</Project>
```

> `Version="*-*"` resolves the latest stable-or-preview from NuGet. Pin to a specific
> version once a working one is confirmed.

---

## Step 2 — Local DTOs

**File:** `Backend/src/Recipes.McpServer/Http/Dtos.cs`

Local copies of API response shapes. No compile-time reference to backend projects.
All properties use the same JSON field names the API serializes (camelCase via
`JsonSerializerDefaults.Web`).

```csharp
namespace Recipes.McpServer.Http;

// ── Recipes ────────────────────────────────────────────────────────────────
public sealed record RecipeListItem(Guid Id, string Name);

public sealed record RecipeDetail(
    Guid Id,
    string Name,
    IReadOnlyList<IngredientItem> Ingredients,
    IReadOnlyList<StepItem> Steps);

public sealed record IngredientItem(string Name, decimal? Quantity, string? Unit);
public sealed record StepItem(int Order, string Instruction);

// ── Meal plans ─────────────────────────────────────────────────────────────
public sealed record MealPlanSummary(
    Guid   Id,
    string Name,
    Guid   HouseholdId,
    string HouseholdName,
    int    EntryCount);

public sealed record MealPlanDetail(
    Guid   Id,
    string Name,
    Guid   HouseholdId,
    string HouseholdName,
    IReadOnlyList<MealPlanEntryItem> Entries);

public sealed record MealPlanEntryItem(
    Guid   Id,
    Guid   BaseRecipeId,
    string BaseRecipeName,
    string PlannedDate,        // DateOnly serialised as "YYYY-MM-DD"
    int    MealType,
    int    Scope,
    IReadOnlyList<AssignmentItem> Assignments);

public sealed record AssignmentItem(
    Guid    PersonId,
    string  PersonName,
    Guid    AssignedRecipeId,
    string  AssignedRecipeName,
    Guid?   RecipeVariationId,
    string? RecipeVariationName,
    decimal PortionMultiplier,
    string? Notes);

// ── Shopping lists ─────────────────────────────────────────────────────────
public sealed record ShoppingListSummary(
    Guid   Id,
    string Name,
    IReadOnlyList<ShoppingItemSummary> Items);

public sealed record ShoppingItemSummary(
    Guid    Id,
    Guid    ProductId,
    string  ProductName,
    decimal Quantity,
    string  Unit,
    bool    IsPurchased,
    string? Notes,
    int     SourceType,
    Guid?   SourceReferenceId);

// ── Expenses ───────────────────────────────────────────────────────────────
public sealed record MonthlyReport(
    int     Year,
    int     Month,
    decimal TotalAmount,
    string  Currency,
    int     ExpenseCount,
    decimal AverageExpenseAmount,
    string? TopCategory,
    decimal FoodPercentage,
    LargestExpenseItem? LargestExpense,
    IReadOnlyList<CategoryBreakdown> Categories);

public sealed record LargestExpenseItem(
    decimal Amount,
    string  Description,
    string  ExpenseDate,   // DateOnly → "YYYY-MM-DD"
    string  Category);

public sealed record CategoryBreakdown(
    string  Category,
    decimal Amount,
    decimal Percentage);

// ── Households ─────────────────────────────────────────────────────────────
public sealed record HouseholdSummary(Guid Id, string Name, int MemberCount);

public sealed record HouseholdDetail(
    Guid   Id,
    string Name,
    IReadOnlyList<MemberItem> Members);

public sealed record MemberItem(
    Guid              PersonId,
    string            PersonName,
    IReadOnlyList<int> DietaryPreferences,
    IReadOnlyList<int> HealthConcerns,
    string?           Notes);
```

---

## Step 3 — RecipesApiClient

**File:** `Backend/src/Recipes.McpServer/Http/RecipesApiClient.cs`

Typed `HttpClient`. Base address comes from DI. `GetAsync<T>` returns `null` on 404;
throws on other non-2xx. `PostAsync` (for write operations) throws on any non-2xx.

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;

namespace Recipes.McpServer.Http;

public sealed class RecipesApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // ── Recipes ───────────────────────────────────────────────────────────
    public Task<List<RecipeListItem>> ListRecipesAsync(CancellationToken ct = default)
        => GetListAsync<RecipeListItem>("/api/recipes", ct);

    public Task<RecipeDetail?> GetRecipeAsync(string id, CancellationToken ct = default)
        => GetAsync<RecipeDetail>($"/api/recipes/{id}", ct);

    public Task<List<RecipeListItem>> SearchRecipesByIngredientAsync(string ingredient, CancellationToken ct = default)
        => GetListAsync<RecipeListItem>(
            $"/api/recipes/search?ingredient={Uri.EscapeDataString(ingredient)}", ct);

    // ── Meal plans ────────────────────────────────────────────────────────
    public Task<List<MealPlanSummary>> ListMealPlansAsync(CancellationToken ct = default)
        => GetListAsync<MealPlanSummary>("/api/meal-plans", ct);

    public Task<MealPlanDetail?> GetMealPlanAsync(string id, CancellationToken ct = default)
        => GetAsync<MealPlanDetail>($"/api/meal-plans/{id}", ct);

    // ── Shopping lists ────────────────────────────────────────────────────
    public Task<List<ShoppingListSummary>> ListShoppingListsAsync(CancellationToken ct = default)
        => GetListAsync<ShoppingListSummary>("/api/shopping-lists", ct);

    public Task<ShoppingListSummary?> GetShoppingListAsync(string id, CancellationToken ct = default)
        => GetAsync<ShoppingListSummary>($"/api/shopping-lists/{id}", ct);

    public Task MarkItemPendingAsync(string listId, string itemId, CancellationToken ct = default)
        => PostEmptyAsync($"/api/shopping-lists/{listId}/items/{itemId}/pending", ct);

    public Task MarkItemPurchasedAsync(string listId, string itemId, CancellationToken ct = default)
        => PostEmptyAsync($"/api/shopping-lists/{listId}/items/{itemId}/purchase", ct);

    // ── Expenses ──────────────────────────────────────────────────────────
    public Task<MonthlyReport?> GetMonthlyReportAsync(int year, int month, CancellationToken ct = default)
        => GetAsync<MonthlyReport>($"/api/expenses/monthly-report?year={year}&month={month}", ct);

    // ── Households ────────────────────────────────────────────────────────
    public Task<List<HouseholdSummary>> ListHouseholdsAsync(CancellationToken ct = default)
        => GetListAsync<HouseholdSummary>("/api/households", ct);

    public Task<HouseholdDetail?> GetHouseholdAsync(string id, CancellationToken ct = default)
        => GetAsync<HouseholdDetail>($"/api/households/{id}", ct);

    // ── Helpers ───────────────────────────────────────────────────────────
    private async Task<T?> GetAsync<T>(string path, CancellationToken ct)
    {
        var response = await http.GetAsync(path, ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return default;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(Json, ct);
    }

    private async Task<List<T>> GetListAsync<T>(string path, CancellationToken ct)
    {
        var response = await http.GetAsync(path, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<T>>(Json, ct) ?? [];
    }

    private async Task PostEmptyAsync(string path, CancellationToken ct)
    {
        var response = await http.PostAsync(path, content: null, ct);
        response.EnsureSuccessStatusCode();
    }
}
```

---

## Step 4 — Program.cs

**File:** `Backend/src/Recipes.McpServer/Program.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Recipes.McpServer.Http;

var builder = Host.CreateApplicationBuilder(args);

var apiBaseUrl = builder.Configuration["RecipesApi:BaseUrl"] ?? "http://localhost:5000";

builder.Services.AddHttpClient<RecipesApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly(typeof(Program).Assembly)
    .WithResourcesFromAssembly(typeof(Program).Assembly)
    .WithPromptsFromAssembly(typeof(Program).Assembly);

await builder.Build().RunAsync();
```

**`appsettings.json`:**

```json
{
  "RecipesApi": {
    "BaseUrl": "http://localhost:5000"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  }
}
```

> Suppress `Information`-level host logs to stdout — they would corrupt the MCP
> stdio protocol. Warning-level only (or redirect to stderr if needed).

---

## Step 5 — Tools

All tool classes use constructor injection of `RecipesApiClient`. The MCP SDK
resolves them from DI.

### `Tools/RecipeTools.cs`

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;
using Recipes.McpServer.Http;

namespace Recipes.McpServer.Tools;

[McpServerToolType]
public sealed class RecipeTools(RecipesApiClient client)
{
    [McpServerTool(Name = "list_recipes")]
    [Description("List all recipes. Returns id and name for each recipe.")]
    public Task<List<RecipeListItem>> ListRecipesAsync(CancellationToken ct)
        => client.ListRecipesAsync(ct);

    [McpServerTool(Name = "get_recipe")]
    [Description("Get full recipe details: ingredients with quantities and units, preparation steps.")]
    public Task<RecipeDetail?> GetRecipeAsync(
        [Description("Recipe id (GUID).")] string id,
        CancellationToken ct)
        => client.GetRecipeAsync(id, ct);

    [McpServerTool(Name = "search_recipes_by_ingredient")]
    [Description("Find recipes that contain a specific ingredient.")]
    public Task<List<RecipeListItem>> SearchRecipesByIngredientAsync(
        [Description("Ingredient name to search for, e.g. 'chicken' or 'garlic'.")] string ingredient,
        CancellationToken ct)
        => client.SearchRecipesByIngredientAsync(ingredient, ct);
}
```

### `Tools/MealPlanTools.cs`

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;
using Recipes.McpServer.Http;

namespace Recipes.McpServer.Tools;

[McpServerToolType]
public sealed class MealPlanTools(RecipesApiClient client)
{
    [McpServerTool(Name = "list_meal_plans")]
    [Description("List all meal plans with their household and entry count.")]
    public Task<List<MealPlanSummary>> ListMealPlansAsync(CancellationToken ct)
        => client.ListMealPlansAsync(ct);

    [McpServerTool(Name = "get_meal_plan")]
    [Description("Get full meal plan details: all entries with dates, meal types, recipes, and per-person assignments.")]
    public Task<MealPlanDetail?> GetMealPlanAsync(
        [Description("Meal plan id (GUID).")] string id,
        CancellationToken ct)
        => client.GetMealPlanAsync(id, ct);
}
```

### `Tools/ShoppingListTools.cs`

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;
using Recipes.McpServer.Http;

namespace Recipes.McpServer.Tools;

[McpServerToolType]
public sealed class ShoppingListTools(RecipesApiClient client)
{
    [McpServerTool(Name = "list_shopping_lists")]
    [Description("List all shopping lists with their items.")]
    public Task<List<ShoppingListSummary>> ListShoppingListsAsync(CancellationToken ct)
        => client.ListShoppingListsAsync(ct);

    [McpServerTool(Name = "get_shopping_list")]
    [Description("Get a shopping list's full item details: product names, quantities, units, and purchased status.")]
    public Task<ShoppingListSummary?> GetShoppingListAsync(
        [Description("Shopping list id (GUID).")] string id,
        CancellationToken ct)
        => client.GetShoppingListAsync(id, ct);

    [McpServerTool(Name = "mark_item_pending")]
    [Description("Mark a previously purchased shopping list item back to pending (not yet bought).")]
    public async Task<string> MarkItemPendingAsync(
        [Description("Shopping list id (GUID).")] string shoppingListId,
        [Description("Item id (GUID).")] string itemId,
        CancellationToken ct)
    {
        await client.MarkItemPendingAsync(shoppingListId, itemId, ct);
        return "Item marked as pending.";
    }

    [McpServerTool(Name = "mark_item_purchased")]
    [Description("Mark a shopping list item as purchased.")]
    public async Task<string> MarkItemPurchasedAsync(
        [Description("Shopping list id (GUID).")] string shoppingListId,
        [Description("Item id (GUID).")] string itemId,
        CancellationToken ct)
    {
        await client.MarkItemPurchasedAsync(shoppingListId, itemId, ct);
        return "Item marked as purchased.";
    }
}
```

### `Tools/ExpenseTools.cs`

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;
using Recipes.McpServer.Http;

namespace Recipes.McpServer.Tools;

[McpServerToolType]
public sealed class ExpenseTools(RecipesApiClient client)
{
    [McpServerTool(Name = "get_monthly_expense_report")]
    [Description("Get the monthly expense report: total spend, category breakdown, largest expense, food percentage.")]
    public Task<MonthlyReport?> GetMonthlyExpenseReportAsync(
        [Description("Year, e.g. 2026.")] int year,
        [Description("Month (1–12).")] int month,
        CancellationToken ct)
        => client.GetMonthlyReportAsync(year, month, ct);
}
```

### `Tools/HouseholdTools.cs`

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;
using Recipes.McpServer.Http;

namespace Recipes.McpServer.Tools;

[McpServerToolType]
public sealed class HouseholdTools(RecipesApiClient client)
{
    [McpServerTool(Name = "list_households")]
    [Description("List all households with member counts.")]
    public Task<List<HouseholdSummary>> ListHouseholdsAsync(CancellationToken ct)
        => client.ListHouseholdsAsync(ct);

    [McpServerTool(Name = "get_household")]
    [Description("Get household details including all members, their dietary preferences, and health concerns.")]
    public Task<HouseholdDetail?> GetHouseholdAsync(
        [Description("Household id (GUID).")] string id,
        CancellationToken ct)
        => client.GetHouseholdAsync(id, ct);
}
```

---

## Step 6 — Resources

**File:** `Backend/src/Recipes.McpServer/Resources/RecipeAppResources.cs`

Resources return formatted markdown strings. URI template parameters become method
parameters with matching names.

```csharp
using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;
using Recipes.McpServer.Http;

namespace Recipes.McpServer.Resources;

[McpServerResourceType]
public sealed class RecipeAppResources(RecipesApiClient client)
{
    [McpServerResource(UriTemplate = "recipe://{id}", Name = "Recipe", MimeType = "text/plain")]
    [Description("Formatted markdown view of a recipe with ingredients and preparation steps.")]
    public async Task<string> GetRecipeResourceAsync(string id, CancellationToken ct)
    {
        var recipe = await client.GetRecipeAsync(id, ct);
        if (recipe is null) return $"Recipe '{id}' not found.";

        var sb = new StringBuilder();
        sb.AppendLine($"# {recipe.Name}");
        sb.AppendLine();
        sb.AppendLine("## Ingredients");
        foreach (var i in recipe.Ingredients)
        {
            var qty  = i.Quantity.HasValue ? $"{i.Quantity} " : "";
            var unit = i.Unit is not null ? $"{i.Unit} " : "";
            sb.AppendLine($"- {qty}{unit}{i.Name}");
        }
        sb.AppendLine();
        sb.AppendLine("## Steps");
        foreach (var s in recipe.Steps.OrderBy(x => x.Order))
            sb.AppendLine($"{s.Order}. {s.Instruction}");

        return sb.ToString();
    }

    [McpServerResource(UriTemplate = "meal-plan://{id}", Name = "Meal Plan", MimeType = "text/plain")]
    [Description("Week-view table of a meal plan: date, meal type, recipe, and person assignments.")]
    public async Task<string> GetMealPlanResourceAsync(string id, CancellationToken ct)
    {
        var plan = await client.GetMealPlanAsync(id, ct);
        if (plan is null) return $"Meal plan '{id}' not found.";

        var mealTypeLabel = (int t) => t switch
        {
            1 => "Breakfast", 2 => "Lunch", 3 => "Dinner", 4 => "Snack", _ => t.ToString()
        };

        var sb = new StringBuilder();
        sb.AppendLine($"# {plan.Name}  ({plan.HouseholdName})");
        sb.AppendLine();
        sb.AppendLine("| Date       | Meal      | Recipe                | Assigned to |");
        sb.AppendLine("|------------|-----------|-----------------------|-------------|");

        foreach (var entry in plan.Entries.OrderBy(e => e.PlannedDate))
        {
            var assignees = entry.Assignments.Count == 0
                ? "—"
                : string.Join(", ", entry.Assignments.Select(a =>
                    $"{a.PersonName}" + (a.PortionMultiplier != 1 ? $" (×{a.PortionMultiplier})" : "")));
            sb.AppendLine($"| {entry.PlannedDate,-10} | {mealTypeLabel(entry.MealType),-9} | {entry.BaseRecipeName,-21} | {assignees} |");
        }

        return sb.ToString();
    }

    [McpServerResource(UriTemplate = "shopping-list://{id}", Name = "Shopping List", MimeType = "text/plain")]
    [Description("Shopping list grouped into pending and purchased sections.")]
    public async Task<string> GetShoppingListResourceAsync(string id, CancellationToken ct)
    {
        var list = await client.GetShoppingListAsync(id, ct);
        if (list is null) return $"Shopping list '{id}' not found.";

        var sb = new StringBuilder();
        sb.AppendLine($"# {list.Name}");
        sb.AppendLine();

        var pending   = list.Items.Where(i => !i.IsPurchased).ToList();
        var purchased = list.Items.Where(i =>  i.IsPurchased).ToList();

        sb.AppendLine("## Pending");
        if (pending.Count == 0)
            sb.AppendLine("_(nothing pending)_");
        else
            foreach (var item in pending)
                sb.AppendLine($"- [ ] {item.Quantity} {item.Unit} {item.ProductName}");

        sb.AppendLine();
        sb.AppendLine("## Purchased");
        if (purchased.Count == 0)
            sb.AppendLine("_(nothing purchased)_");
        else
            foreach (var item in purchased)
                sb.AppendLine($"- [x] {item.Quantity} {item.Unit} {item.ProductName}");

        return sb.ToString();
    }
}
```

---

## Step 7 — Prompts

**File:** `Backend/src/Recipes.McpServer/Prompts/RecipeAppPrompts.cs`

Prompts return `IEnumerable<PromptMessage>`. Each prompt is a pre-filled user message
that starts a focused Claude conversation.

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol.Types;

namespace Recipes.McpServer.Prompts;

[McpServerPromptType]
public sealed class RecipeAppPrompts
{
    [McpServerPrompt(Name = "plan_week_for_household")]
    [Description("Set up a conversation to plan a 7-day meal schedule for a household.")]
    public IEnumerable<PromptMessage> PlanWeekForHousehold(
        [Description("Household id (GUID).")] string householdId,
        [Description("Start date for the week (YYYY-MM-DD).")] string startDate)
    {
        yield return new PromptMessage(
            Role.User,
            new TextContent(
                $"""
                I'd like to plan meals for household {householdId} starting {startDate}.

                Please:
                1. Use get_household to look up the members and their dietary preferences.
                2. Use list_recipes to see what recipes are available.
                3. Suggest a 7-day meal plan covering breakfast, lunch, and dinner,
                   taking dietary restrictions into account.
                4. For each day, name the recipe and note which household members it suits.
                """));
    }

    [McpServerPrompt(Name = "weekly_budget_review")]
    [Description("Set up a conversation to review and analyse monthly food expenses.")]
    public IEnumerable<PromptMessage> WeeklyBudgetReview(
        [Description("Year, e.g. 2026.")] string year,
        [Description("Month number (1–12).")] string month)
    {
        yield return new PromptMessage(
            Role.User,
            new TextContent(
                $"""
                Please review my food expenses for {year}-{month.PadLeft(2, '0')}.

                Use get_monthly_expense_report to fetch the data, then:
                1. Summarise total spend, top categories, and food percentage.
                2. Compare food spend to the average expense amount.
                3. Highlight the largest single expense.
                4. Suggest two or three practical ways to reduce spend next month.
                """));
    }

    [McpServerPrompt(Name = "substitute_ingredient_for_recipe")]
    [Description("Set up a conversation to find substitutes for an ingredient in a specific recipe.")]
    public IEnumerable<PromptMessage> SubstituteIngredientForRecipe(
        [Description("Recipe id (GUID).")] string recipeId,
        [Description("Name of the ingredient to substitute.")] string ingredientName)
    {
        yield return new PromptMessage(
            Role.User,
            new TextContent(
                $"""
                I need to substitute '{ingredientName}' in recipe {recipeId}.

                Please use get_recipe to fetch the full recipe, then suggest three
                substitutes for '{ingredientName}'. For each substitute:
                - Name the replacement ingredient.
                - Explain how it changes the flavour or texture.
                - Note any quantity adjustment needed.
                """));
    }
}
```

---

## Step 8 — Registration files

### `.claude/mcp.json` (repo root)

**File:** `.claude/mcp.json`

```json
{
  "mcpServers": {
    "recipes-app": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "Backend/src/Recipes.McpServer",
        "--no-launch-profile"
      ],
      "env": {
        "RECIPES_API_BASE_URL": "http://localhost:5000"
      }
    }
  }
}
```

---

## Step 9 — README

**File:** `Backend/src/Recipes.McpServer/README.md`

Include:
1. What the server exposes (tools table, resources table, prompts table).
2. Prerequisites: .NET 10, Recipes API running.
3. Run locally: `dotnet run --project Backend/src/Recipes.McpServer`.
4. Verify: `npx @modelcontextprotocol/inspector dotnet run --project Backend/src/Recipes.McpServer`.
5. Claude Code registration: already in `.claude/mcp.json` — restart Claude Code.
6. Claude Desktop registration snippet.

---

## Step 10 — CCAF documentation

**File:** `Backend/Docs/CCAF/F4-mcp-server-stdio.md`

Follow the standard CCAF doc structure:
- What this implements (one paragraph).
- Subtopics table (tool authoring, resource authoring, prompt authoring, stdio transport, registration).
- Key decisions: thin HTTP client vs direct DB; why write tools are limited to mark only; why prompts are user messages not system prompts; stdio vs HTTP (HTTP is F6).

---

## Implementation checklist

- [ ] Step 1 — `dotnet new` + `dotnet add package` × 3 + `dotnet sln add`
- [ ] Step 2 — `Http/Dtos.cs`
- [ ] Step 3 — `Http/RecipesApiClient.cs`
- [ ] Step 4 — `Program.cs` + `appsettings.json`
- [ ] Step 5a — `Tools/RecipeTools.cs`
- [ ] Step 5b — `Tools/MealPlanTools.cs`
- [ ] Step 5c — `Tools/ShoppingListTools.cs`
- [ ] Step 5d — `Tools/ExpenseTools.cs`
- [ ] Step 5e — `Tools/HouseholdTools.cs`
- [ ] Step 6 — `Resources/RecipeAppResources.cs`
- [ ] Step 7 — `Prompts/RecipeAppPrompts.cs`
- [ ] Step 8 — `.claude/mcp.json`
- [ ] Step 9 — `Backend/src/Recipes.McpServer/README.md`
- [ ] Step 10 — `Backend/Docs/CCAF/F4-mcp-server-stdio.md`
- [ ] `dotnet build Backend/Recipes.sln` passes
- [ ] `dotnet run --project Backend/src/Recipes.McpServer` starts without error (API running)
- [ ] MCP Inspector lists 12 tools, 3 resources, 3 prompts

## SDK API note

The exact attribute names (`[McpServerToolType]`, `[McpServerTool]`, etc.) and
namespace (`ModelContextProtocol.Server`) are based on the C# SDK as of mid-2025.
If the resolved package version uses different names, check:

```
dotnet add package ModelContextProtocol --prerelease
dotnet list Backend/src/Recipes.McpServer package
```

Then inspect the package's `README` or source for the correct attribute and namespace
names before writing tool/resource/prompt files.
