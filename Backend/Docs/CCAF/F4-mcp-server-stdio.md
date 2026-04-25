# F4 — RecipesApp MCP Server (stdio transport)

## What this implements

A standalone `Recipes.McpServer` console project that exposes the RecipesApp REST API
as an MCP server over stdio transport. It registers **tools** (12 callable actions),
**resources** (3 URI-template readable data sources), and **prompts** (3 reusable message
templates) using the official `ModelContextProtocol` C# SDK v1.2.0. The server is
registerable with both Claude Code (via `.claude/mcp.json`) and Claude Desktop
(via `claude_desktop_config.json`). Existing API and backend projects are not modified.

---

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| MCP server authoring — tools | `Tools/RecipeTools.cs`, `MealPlanTools.cs`, `ShoppingListTools.cs`, `ExpenseTools.cs`, `HouseholdTools.cs` |
| MCP server authoring — resources | `Resources/RecipeAppResources.cs` — `recipe://`, `meal-plan://`, `shopping-list://` URI templates |
| MCP server authoring — prompts | `Prompts/RecipeAppPrompts.cs` — 3 reusable conversation starters |
| MCP stdio transport | `WithStdioServerTransport()` in `Program.cs`; `Logging.ClearProviders()` keeps stdout clean |
| MCP server registration with Claude Code | `.claude/mcp.json` at repo root, discovered automatically |
| MCP server registration with Claude Desktop | `README.md` snippet for `claude_desktop_config.json` |
| Tool descriptions as AI guidance | Every `[McpServerTool]` and `[McpServerResource]` carries a `[Description]` attribute that appears in the tool schema Claude receives |
| Parameter descriptions | Every tool/resource parameter has `[Description]` so Claude knows what values to pass |

---

## Key decisions

### Thin HTTP-client architecture
The MCP server does **not** reference `Recipes.Domain`, `Recipes.Application`, or
`Recipes.Infrastructure`. It communicates with the running API through `RecipesApiClient`
(a typed `HttpClient`). Benefits: independently deployable, no duplicate business logic,
works even if the API is behind a firewall or on a different host.

### stdio transport + `ClearProviders()`
The MCP stdio transport uses stdin/stdout as its byte pipe. Any console logging that leaks
to stdout corrupts the MCP protocol stream. `builder.Logging.ClearProviders()` suppresses
all log output at startup, making the server safe to run as a Claude subprocess.

### `[Description]` attributes as the tool-schema contract
The C# SDK derives the JSON schema for each tool from the method signature and the
`[Description]` attributes on the method and its parameters. These descriptions are
exactly what Claude sees in the `tools` array — they are the primary guidance for when
and how to call each tool. Clear, action-oriented descriptions reduce hallucinated
arguments significantly.

### Mutating tools intentionally limited
Only `mark_item_pending` and `mark_item_purchased` mutate state. Broader write operations
(create recipe, create meal plan, etc.) are left out — the risk of Claude autonomously
creating or deleting data in a cooking app is disproportionate to the benefit. This aligns
with the principle of *minimum necessary authority* for MCP tools.

### Local DTO copies in `Http/Dtos.cs`
Mirroring only the fields the MCP server needs, rather than re-using Application-layer
types, keeps the project free of compile-time dependencies on the backend. The API is the
authoritative contract — if the API adds a field, the MCP server is updated independently.

### Prompts return pre-filled `PromptMessage` arrays
Each prompt builds a user message that instructs Claude to call specific tools and present
results in a defined format. This is the standard MCP prompt pattern: the prompt is a
conversation bootstrap, not a system instruction. The arguments (e.g. `householdId`,
`startDate`) are embedded in the message text so Claude has the right context immediately.

---

## Comparison: MCP tools vs. JSON-schema-in-prompt (existing approach)

| Concern | Existing approach (F1–F3 features) | MCP tools (F4) |
|---|---|---|
| Schema location | Embedded in system prompt as JSON text | `[Description]` attributes on C# methods |
| Transport | Direct HTTP to Claude API | stdio (subprocess) or HTTP transport |
| Who calls the tool | Backend orchestrator in a loop | Claude calls tools, results returned via MCP |
| Tool discoverability | Only known to the app calling Claude | Any MCP-compatible client (Claude Code, Desktop) |
| Mutation safety | Controlled by orchestrator logic | Controlled by which tools are exposed |
| Testing | Unit-testable agent loop | MCP Inspector for headless verification |
