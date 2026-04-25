# F4 — RecipesApp MCP Server (stdio transport)

## Summary

A new standalone console project `Backend/src/Recipes.McpServer/` that exposes the
RecipesApp REST API as an MCP server over stdio transport. Provides **tools** (callable
actions), **resources** (readable data at URIs), and **prompts** (reusable message
templates) using the official `ModelContextProtocol` C# SDK.

Registerable with Claude Desktop and Claude Code via config files committed to the
repo. Existing API and backend code are **not modified**.

---

## CCAF subtopics covered

| Subtopic | Where |
|---|---|
| MCP server authoring — tools | `RecipeTools`, `MealPlanTools`, `ShoppingListTools`, `ExpenseTools`, `HouseholdTools` |
| MCP server authoring — resources | `RecipeAppResources` — three URI-template resources |
| MCP server authoring — prompts | `RecipeAppPrompts` — three reusable prompt templates |
| MCP stdio transport | `WithStdioServerTransport()` in `Program.cs` |
| MCP server registration with Claude Code | `.claude/mcp.json` committed to repo root |
| MCP server registration with Claude Desktop | `README.md` snippet for `claude_desktop_config.json` |

---

## Architecture

The MCP server is a **thin HTTP client** over the existing REST API. It does not
reference `Recipes.Domain`, `Recipes.Application`, or `Recipes.Infrastructure` directly
— it communicates with the running API via a typed `RecipesApiClient` (named
`HttpClient`). This keeps the server deployable independently and avoids duplicating
business logic.

```
Claude Desktop / Claude Code
       │  stdin/stdout
       ▼
Recipes.McpServer (console, stdio)
       │  HTTP
       ▼
Recipes.Api  (http://localhost:5000)
       │
       ▼
SQL Server
```

**Prerequisite:** the Recipes API must be running when the MCP server is started.
The API base URL is configured via `RECIPES_API_BASE_URL` environment variable
(default: `http://localhost:5000`).

---

## Project

**Location:** `Backend/src/Recipes.McpServer/`
**Type:** Console app, `net10.0`

**NuGet packages:**
- `ModelContextProtocol` — official C# MCP server SDK
- `Microsoft.Extensions.Hosting` — generic host
- `Microsoft.Extensions.Http` — named `HttpClient`
- `System.Text.Json` — JSON serialization

**Not referenced:** `Recipes.Domain`, `Recipes.Application`, `Recipes.Infrastructure`.

---

## Tools (12 total)

All tools are read-only except `mark_item_pending` and `mark_item_purchased`.

### RecipeTools

| Tool name | REST call | Description |
|---|---|---|
| `list_recipes` | `GET /api/recipes` | List all recipes (id + name). |
| `get_recipe` | `GET /api/recipes/{id}` | Full details: ingredients, steps. |
| `search_recipes_by_ingredient` | `GET /api/recipes/search?ingredient=...` | Recipes containing the given ingredient. |

### MealPlanTools

| Tool name | REST call | Description |
|---|---|---|
| `list_meal_plans` | `GET /api/meal-plans` | List all meal plans (id + name). |
| `get_meal_plan` | `GET /api/meal-plans/{id}` | Full details: entries, assignments, dates. |

### ShoppingListTools

| Tool name | REST call | Description |
|---|---|---|
| `list_shopping_lists` | `GET /api/shopping-lists` | List all shopping lists (id + name). |
| `get_shopping_list` | `GET /api/shopping-lists/{id}` | Full details: items, quantities, purchased status. |
| `mark_item_pending` | `POST /api/shopping-lists/{id}/items/{itemId}/pending` | Mark a purchased item back to pending. |
| `mark_item_purchased` | `POST /api/shopping-lists/{id}/items/{itemId}/purchase` | Mark an item as purchased. |

### ExpenseTools

| Tool name | REST call | Description |
|---|---|---|
| `get_monthly_expense_report` | `GET /api/expenses/monthly-report?year=&month=` | Monthly totals, categories, top expense. |

### HouseholdTools

| Tool name | REST call | Description |
|---|---|---|
| `list_households` | `GET /api/households` | List all households (id + name). |
| `get_household` | `GET /api/households/{id}` | Household details including members. |

---

## Resources (3 total)

Resources are addressable by URI and return formatted markdown text (`text/plain`).

| URI template | Description |
|---|---|
| `recipe://{id}` | Recipe name, servings, ingredient list, numbered steps. |
| `meal-plan://{id}` | Week-view table: date × meal type, recipe name, assignees. |
| `shopping-list://{id}` | Grouped list: pending items / purchased items, quantities. |

---

## Prompts (3 total)

Prompts return a `PromptMessage[]` — a pre-filled user message that bootstraps a
conversation with context and instructions.

### `plan_week_for_household`

**Arguments:** `householdId` (string), `startDate` (string, YYYY-MM-DD)

**Returns:** User message instructing Claude to look up household members and available
recipes, then suggest a 7-day meal plan covering the configured meal types.

### `weekly_budget_review`

**Arguments:** `year` (string), `month` (string, 1–12)

**Returns:** User message instructing Claude to retrieve the monthly expense report and
provide a budget analysis: total spend, top categories, suggestions for next month.

### `substitute_ingredient_for_recipe`

**Arguments:** `recipeId` (string), `ingredientName` (string)

**Returns:** User message instructing Claude to fetch the full recipe, then suggest
three substitutes for the named ingredient with reasoning for each.

---

## API contract of `RecipesApiClient`

Typed `HttpClient` with base address from config. Each method returns a typed DTO or
`null` on 404. Non-2xx responses other than 404 throw `HttpRequestException`.

```
Task<IReadOnlyList<RecipeListItemDto>> ListRecipesAsync(CancellationToken ct)
Task<RecipeDto?> GetRecipeAsync(string id, CancellationToken ct)
Task<IReadOnlyList<RecipeListItemDto>> SearchRecipesByIngredientAsync(string ingredient, CancellationToken ct)

Task<IReadOnlyList<MealPlanListItemDto>> ListMealPlansAsync(CancellationToken ct)
Task<MealPlanDetailsDto?> GetMealPlanAsync(string id, CancellationToken ct)

Task<IReadOnlyList<ShoppingListSummaryDto>> ListShoppingListsAsync(CancellationToken ct)
Task<ShoppingListDetailsDto?> GetShoppingListAsync(string id, CancellationToken ct)
Task MarkItemPendingAsync(string shoppingListId, string itemId, CancellationToken ct)
Task MarkItemPurchasedAsync(string shoppingListId, string itemId, CancellationToken ct)

Task<MonthlyExpenseReportDto?> GetMonthlyExpenseReportAsync(int year, int month, CancellationToken ct)

Task<IReadOnlyList<HouseholdSummaryDto>> ListHouseholdsAsync(CancellationToken ct)
Task<HouseholdDetailsDto?> GetHouseholdAsync(string id, CancellationToken ct)
```

DTOs are defined in `Recipes.McpServer/Http/Dtos.cs` — copies of the relevant
Application-layer DTOs. They are kept local to the MCP server project so it has no
compile-time dependency on the backend.

---

## Configuration

| Key | Env variable | Default | Purpose |
|---|---|---|---|
| `RecipesApi:BaseUrl` | `RECIPES_API_BASE_URL` | `http://localhost:5000` | API base address |

Read via `IConfiguration` in `Program.cs`.

---

## Registration files

### `.claude/mcp.json` (committed to repo root)

```json
{
  "mcpServers": {
    "recipes-app": {
      "command": "dotnet",
      "args": [
        "run",
        "--project", "Backend/src/Recipes.McpServer",
        "--no-launch-profile"
      ],
      "env": {
        "RECIPES_API_BASE_URL": "http://localhost:5000"
      }
    }
  }
}
```

### Claude Desktop snippet (in `Backend/src/Recipes.McpServer/README.md`)

```json
{
  "mcpServers": {
    "recipes-app": {
      "command": "dotnet",
      "args": [
        "run",
        "--project", "/absolute/path/to/RecipesApp/Backend/src/Recipes.McpServer",
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

## Resource markdown formats

### `recipe://{id}`

```
# {name}

**Servings:** {servings}

## Ingredients
- {quantity} {unit} {name}
...

## Steps
1. {instruction}
...
```

### `meal-plan://{id}`

```
# {name}  ({householdName})

| Date       | Meal    | Recipe              | Assigned to         |
|------------|---------|---------------------|---------------------|
| 2026-05-01 | Dinner  | Spaghetti Carbonara | Alice (×1), Bob (×1)|
...
```

### `shopping-list://{id}`

```
# {name}

## Pending
- [ ] {quantity} {unit} {productName}
...

## Purchased
- [x] {quantity} {unit} {productName}
...
```

---

## Acceptance criteria

1. `dotnet build Backend/Recipes.sln` passes with the new project included.
2. `dotnet run --project Backend/src/Recipes.McpServer` starts and waits on stdin without error (API running).
3. MCP Inspector (`npx @modelcontextprotocol/inspector dotnet run --project ...`) lists all 12 tools, 3 resources, 3 prompts.
4. `list_recipes` tool call returns the current recipe list from the running API.
5. `recipe://{validId}` resource returns formatted markdown with ingredients and steps.
6. `plan_week_for_household` prompt returns a user message referencing the provided `householdId` and `startDate`.
7. `.claude/mcp.json` is committed and discovered automatically by Claude Code in the repo.
8. `mark_item_purchased` + `mark_item_pending` round-trip: item toggles status via two sequential tool calls.

---

## Out of scope

- Write tools for creating recipes, meal plans, or expenses (too broad for safe Claude automation).
- Authentication on the stdio transport (added in F6 for HTTP transport).
- Streaming or subscription resources.
- A test project for the MCP server (integration-tested manually via MCP Inspector per AC#3–6).
- Angular/React UI changes.

---

## Files to create

| Path | Purpose |
|---|---|
| `Backend/src/Recipes.McpServer/Recipes.McpServer.csproj` | Console app, net10.0, ModelContextProtocol SDK |
| `Backend/src/Recipes.McpServer/Program.cs` | Host setup: `AddMcpServer`, stdio transport, HttpClient |
| `Backend/src/Recipes.McpServer/Http/RecipesApiClient.cs` | Typed HTTP client wrapping the REST API |
| `Backend/src/Recipes.McpServer/Http/Dtos.cs` | Local DTO copies (no dependency on backend projects) |
| `Backend/src/Recipes.McpServer/Tools/RecipeTools.cs` | 3 recipe tools |
| `Backend/src/Recipes.McpServer/Tools/MealPlanTools.cs` | 2 meal plan tools |
| `Backend/src/Recipes.McpServer/Tools/ShoppingListTools.cs` | 4 shopping list tools |
| `Backend/src/Recipes.McpServer/Tools/ExpenseTools.cs` | 1 expense tool |
| `Backend/src/Recipes.McpServer/Tools/HouseholdTools.cs` | 2 household tools |
| `Backend/src/Recipes.McpServer/Resources/RecipeAppResources.cs` | 3 resources |
| `Backend/src/Recipes.McpServer/Prompts/RecipeAppPrompts.cs` | 3 prompts |
| `Backend/src/Recipes.McpServer/README.md` | Setup instructions + Claude Desktop config snippet |
| `.claude/mcp.json` | Claude Code MCP server registration |
| `Backend/Docs/CCAF/F4-mcp-server-stdio.md` | CCAF documentation |

## Files to modify

| Path | Change |
|---|---|
| `Backend/Recipes.sln` | Add `Recipes.McpServer` project |

## Files not modified

All existing backend projects, endpoints, and services are unchanged.
