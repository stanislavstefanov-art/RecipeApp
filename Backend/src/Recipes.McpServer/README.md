# Recipes.McpServer

MCP server that exposes the RecipesApp REST API to AI assistants.
Supports **stdio** (Claude Code / Claude Desktop subprocess) and **HTTP streamable** (remote / Azure) transports.

## Prerequisites

The Recipes API must be running before starting the MCP server:

```bash
dotnet run --project Backend/src/Recipes.Api
```

## Running

### stdio mode (Claude Code / Claude Desktop)

```bash
dotnet run --project Backend/src/Recipes.McpServer --no-launch-profile -- --stdio
```

The `--stdio` flag switches to a generic host that speaks the MCP stdio protocol on stdin/stdout.
Logging is suppressed in this mode to avoid corrupting the protocol pipe.

### HTTP mode (remote / dev)

```bash
dotnet run --project Backend/src/Recipes.McpServer --no-launch-profile
```

Without `--stdio`, the server starts as an ASP.NET Core app.
Default listen address: `http://localhost:5010` (set via `ASPNETCORE_URLS` to avoid conflict with the API on 5000).

| Endpoint | Auth | Description |
|----------|------|-------------|
| `POST /mcp` | Bearer | MCP streamable HTTP — initialize, tool/resource/prompt calls |
| `GET /mcp` | Bearer | SSE stream for server-to-client notifications |
| `DELETE /mcp` | Bearer | Terminate session |
| `GET /health` | None | Liveness probe |

## Configuration

| Key | Env variable | Default | Purpose |
|-----|--------------|---------|---------|
| `RecipesApi:BaseUrl` | `RECIPES_API_BASE_URL` | `http://localhost:5000` | API base address |
| `MCP_SERVER_TOKEN` | `MCP_SERVER_TOKEN` | *(none)* | Bearer token; unset = no auth (dev) |
| `ASPNETCORE_URLS` | `ASPNETCORE_URLS` | `http://localhost:5010` | HTTP listen address |

## Authentication (HTTP mode)

When `MCP_SERVER_TOKEN` is set, every request to `/mcp` must include:

```
Authorization: Bearer <token>
```

If the variable is not set, the server logs a warning and accepts all requests (dev convenience).

```bash
# Start with auth enabled
MCP_SERVER_TOKEN=secret ASPNETCORE_URLS=http://localhost:5010 \
  dotnet run --project Backend/src/Recipes.McpServer --no-launch-profile

# 401 — missing token
curl -s -o /dev/null -w "%{http_code}" http://localhost:5010/mcp

# 200 — valid token
curl -s -o /dev/null -w "%{http_code}" \
  -H "Authorization: Bearer secret" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"0"}}}' \
  http://localhost:5010/mcp
```

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

The `.claude/mcp.json` at the repo root is picked up automatically by Claude Code and uses stdio mode.

### Claude Desktop

Add to `claude_desktop_config.json` (`%APPDATA%\Claude\` on Windows, `~/Library/Application Support/Claude/` on macOS):

```json
{
  "mcpServers": {
    "recipes-app": {
      "command": "dotnet",
      "args": [
        "run",
        "--project", "/absolute/path/to/RecipesApp/Backend/src/Recipes.McpServer",
        "--no-launch-profile",
        "--",
        "--stdio"
      ],
      "env": {
        "RECIPES_API_BASE_URL": "http://localhost:5000"
      }
    }
  }
}
```

### Remote HTTP (MCP Inspector)

```bash
# Start the server
MCP_SERVER_TOKEN=secret ASPNETCORE_URLS=http://localhost:5010 \
  dotnet run --project Backend/src/Recipes.McpServer --no-launch-profile

# Connect with MCP Inspector
npx @modelcontextprotocol/inspector \
  --transport http \
  --url http://localhost:5010/mcp \
  --header "Authorization: Bearer secret"
```

This should list all 12 tools, 3 resources, and 3 prompts.

## Verifying with MCP Inspector (stdio)

```bash
npx @modelcontextprotocol/inspector \
  dotnet run --project Backend/src/Recipes.McpServer --no-launch-profile -- --stdio
```
