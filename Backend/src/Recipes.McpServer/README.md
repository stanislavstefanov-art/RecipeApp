# Recipes.McpServer

MCP server (stdio transport) that exposes the RecipesApp REST API to AI assistants.

## Prerequisites

The Recipes API must be running before starting the MCP server:

```bash
dotnet run --project Backend/src/Recipes.Api
```

## Running

```bash
dotnet run --project Backend/src/Recipes.McpServer --no-launch-profile
```

The `RECIPES_API_BASE_URL` environment variable controls the API base address (default: `http://localhost:5000`).

## Tools (12)

| Tool | Description |
|------|-------------|
| `list_recipes` | List all recipes (id + name) |
| `get_recipe` | Full recipe: ingredients, steps |
| `search_recipes_by_ingredient` | Recipes containing a given ingredient |
| `list_meal_plans` | List all meal plans |
| `get_meal_plan` | Full meal plan: entries, assignments, dates |
| `list_shopping_lists` | List all shopping lists |
| `get_shopping_list` | Full shopping list: items, quantities, purchased status |
| `mark_item_pending` | Mark a purchased item back to pending |
| `mark_item_purchased` | Mark an item as purchased |
| `get_monthly_expense_report` | Monthly totals, categories, top expense |
| `list_households` | List all households |
| `get_household` | Household details including members |

## Resources (3)

| URI | Description |
|-----|-------------|
| `recipe://{id}` | Markdown: name, ingredients, steps |
| `meal-plan://{id}` | Markdown table: date × meal type × recipe × assignees |
| `shopping-list://{id}` | Markdown: pending / purchased items |

## Prompts (3)

| Name | Description |
|------|-------------|
| `plan_week_for_household` | Bootstrap a 7-day meal-planning conversation |
| `weekly_budget_review` | Bootstrap a monthly budget review conversation |
| `substitute_ingredient_for_recipe` | Bootstrap an ingredient substitution conversation |

## Registration

### Claude Code (automatic via `.claude/mcp.json` in repo root)

The `.claude/mcp.json` file at the repo root is picked up automatically by Claude Code.

### Claude Desktop

Add the following to your `claude_desktop_config.json` (usually at
`%APPDATA%\Claude\claude_desktop_config.json` on Windows or
`~/Library/Application Support/Claude/claude_desktop_config.json` on macOS):

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

## Verifying with MCP Inspector

```bash
npx @modelcontextprotocol/inspector dotnet run --project Backend/src/Recipes.McpServer --no-launch-profile
```

This should list all 12 tools, 3 resources, and 3 prompts.
