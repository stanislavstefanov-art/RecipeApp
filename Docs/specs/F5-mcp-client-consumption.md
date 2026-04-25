# F5 — MCP Client Consumption (nutrition lookup)

## Summary

Demonstrates MCP client consumption: the backend calls an external MCP server as part of
an agentic loop. Concretely, a new endpoint `POST /api/recipes/{id}/analyse-nutrition`
runs a Claude agent that proxies the `lookup_nutrition` tool from a local mock MCP server,
then returns a per-ingredient nutrition summary.

No existing endpoints or agents are modified.

---

## CCAF subtopics covered

| Subtopic | Where |
|---|---|
| MCP client consumption | `NutritionMcpClient` — connects to a mock MCP server via stdio |
| Proxying MCP tools into Claude's `tools` array | `NutritionAnalysisAgent` builds tool definitions from the MCP server's tool list at runtime |
| Composing multiple MCP servers | Agent receives tools from two sources: static local tools + MCP-proxied tools |

---

## Architecture

```
POST /api/recipes/{id}/analyse-nutrition
       │  MediatR
       ▼
AnalyseRecipeNutritionHandler
       │
       ▼
NutritionAnalysisAgent  (agentic loop, Claude claude-haiku-4-5)
  ├── static tool: get_recipe_ingredients(recipeId) → from Recipes API
  └── MCP-proxied tool: lookup_nutrition(ingredient, quantity, unit) → from NutritionMcpServer
       │  stdio subprocess
       ▼
NutritionMcpServer  (mcp/nutrition-server/)
```

**Prerequisite:** The Recipes API must be running. The nutrition MCP server is launched as
a subprocess by `NutritionMcpClient`; it does not need to be started separately.

---

## New projects

### `mcp/nutrition-server/` — mock nutrition MCP server

Console app, `net10.0`, `ModelContextProtocol` SDK.

Exposes one tool:

#### `lookup_nutrition`

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `ingredient` | string | Ingredient name (e.g. "spaghetti") |
| `quantity` | number | Numeric amount |
| `unit` | string | Unit of measure (e.g. "g", "ml", "tbsp") |

**Output:** JSON string with stubbed nutritional values:
```json
{
  "ingredient": "spaghetti",
  "quantity": 400,
  "unit": "g",
  "calories": 560,
  "proteinG": 20,
  "carbG": 112,
  "fatG": 2.4,
  "fiberG": 4.8
}
```

Values are deterministic stubs derived from the ingredient name length (to avoid
random output while remaining unique per ingredient). The point is the wiring, not real
nutrition data.

**NuGet:** `ModelContextProtocol`, `Microsoft.Extensions.Hosting`

---

## New backend infrastructure

### `Backend/src/Recipes.Infrastructure/Mcp/NutritionMcpClient.cs`

Wraps `McpClientFactory.CreateAsync()` (stdio transport) to connect to the nutrition
MCP server. Exposes:

```
Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken ct)
Task<string> CallToolAsync(string toolName, object arguments, CancellationToken ct)
IAsyncDisposable
```

The client launches the nutrition server process via:
```
command: dotnet
args:    run --project mcp/nutrition-server --no-launch-profile
```

Path resolved relative to the API's working directory (configurable via
`NutritionMcp:ServerProject` — default: `../../../../mcp/nutrition-server`).

`McpToolDefinition` is a local record:
```csharp
public sealed record McpToolDefinition(string Name, string Description, JsonDocument InputSchema);
```

### `Backend/src/Recipes.Infrastructure/Mcp/IMcpClientHost.cs`

```csharp
public interface IMcpClientHost : IAsyncDisposable
{
    Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken ct);
    Task<string> CallToolAsync(string toolName, object arguments, CancellationToken ct);
}
```

Implemented by `NutritionMcpClient`.

---

## New Application feature

### `AnalyseRecipeNutrition`

**Command:** `AnalyseRecipeNutritionCommand(Guid RecipeId)`

**Handler:** `AnalyseRecipeNutritionHandler`
- Delegates to `INutritionAnalysisAgent.RunAsync(recipeId, ct)`

**Response DTO:** `NutritionAnalysisDto`
```csharp
public sealed record NutritionAnalysisDto(
    Guid RecipeId,
    string RecipeName,
    IReadOnlyList<IngredientNutritionDto> Ingredients,
    NutritionTotalsDto Totals);

public sealed record IngredientNutritionDto(
    string Name,
    decimal Quantity,
    string Unit,
    double Calories,
    double ProteinG,
    double CarbG,
    double FatG,
    double FiberG);

public sealed record NutritionTotalsDto(
    double Calories,
    double ProteinG,
    double CarbG,
    double FatG,
    double FiberG);
```

**Interface:** `INutritionAnalysisAgent` (in `Application/Recipes/AnalyseRecipeNutrition/`)

---

## New API endpoint

```
POST /api/recipes/{id}/analyse-nutrition
```

Route added to `RecipesEndpoints.cs`. Returns `200 NutritionAnalysisDto` or `404`.

---

## Agent design

### `NutritionAnalysisAgent`

Located in `Backend/src/Recipes.Infrastructure/AI/Claude/Agents/NutritionAnalysisAgent.cs`.

**Tools available to Claude:**

| Tool name | Source | Description |
|---|---|---|
| `get_recipe_ingredients` | Static (local) | Calls `GET /api/recipes/{id}` and returns ingredient list |
| `lookup_nutrition` | MCP-proxied | Forwarded to nutrition MCP server |
| `save_nutrition_analysis` | Static (terminal) | Ends the loop; input becomes the result |

**Loop:**
1. Claude calls `get_recipe_ingredients(recipeId)` to get the ingredient list.
2. Claude calls `lookup_nutrition` in parallel for each ingredient (same parallel tool-call
   pattern as F2's `normalize_ingredient`).
3. Claude calls `save_nutrition_analysis` with the aggregated result.

**MCP proxying pattern:**
`NutritionAnalysisAgent` calls `IMcpClientHost.ListToolsAsync()` at startup, converts
`McpToolDefinition` records into `ClaudeToolDefinition` objects, and includes them in the
`tools` array alongside the static tools. When Claude calls `lookup_nutrition`, the agent
dispatches to `IMcpClientHost.CallToolAsync(...)`.

This means Claude's `tools` array is composed at runtime from two sources: the static
`NutritionAnalysisTools` class and the live MCP server tool list.

---

## Configuration

| Key | Env variable | Default | Purpose |
|---|---|---|---|
| `NutritionMcp:ServerProject` | `NUTRITION_MCP_PROJECT` | `../../../../mcp/nutrition-server` | Relative path from working dir to the nutrition MCP server project |

---

## Acceptance criteria

1. `dotnet build Backend/Recipes.sln` passes with the new project included (nutrition server added to sln).
2. `POST /api/recipes/{validId}/analyse-nutrition` returns `200` with nutrition data for each ingredient.
3. MCP Inspector against the nutrition server lists the `lookup_nutrition` tool.
4. Agent trace (log output) shows `lookup_nutrition` being called once per ingredient.
5. Parallel dispatch: all `lookup_nutrition` calls in a single Claude turn are executed with `Task.WhenAll`.
6. `dotnet test` passes (existing tests unaffected; no new tests required for F5).

---

## Out of scope

- Real nutrition data (stubs are sufficient for wiring demo).
- Caching or persistent storage of nutrition results.
- Authentication on the MCP server.
- A test project for the nutrition MCP server.
- Any Angular/React UI changes.

---

## Files to create

| Path | Purpose |
|---|---|
| `mcp/nutrition-server/NutritionServer.csproj` | Console app, net10.0, ModelContextProtocol SDK |
| `mcp/nutrition-server/Program.cs` | Host setup: AddMcpServer, WithStdioServerTransport |
| `mcp/nutrition-server/NutritionTools.cs` | `lookup_nutrition` tool with stub data |
| `Backend/src/Recipes.Infrastructure/Mcp/IMcpClientHost.cs` | Interface |
| `Backend/src/Recipes.Infrastructure/Mcp/McpToolDefinition.cs` | Local record |
| `Backend/src/Recipes.Infrastructure/Mcp/NutritionMcpClient.cs` | Stdio MCP client wrapping McpClientFactory |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Agents/NutritionAnalysisAgent.cs` | Agentic loop |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Agents/NutritionAnalysisToolTypes.cs` | Static tool input/output records |
| `Backend/src/Recipes.Application/Recipes/AnalyseRecipeNutrition/AnalyseRecipeNutritionCommand.cs` | Command + interface |
| `Backend/src/Recipes.Application/Recipes/AnalyseRecipeNutrition/AnalyseRecipeNutritionHandler.cs` | Handler |
| `Backend/src/Recipes.Application/Recipes/AnalyseRecipeNutrition/NutritionAnalysisDto.cs` | Response DTOs |
| `Backend/Docs/CCAF/F5-mcp-client-consumption.md` | CCAF documentation |

## Files to modify

| Path | Change |
|---|---|
| `Backend/Recipes.sln` | Add nutrition-server project |
| `Backend/src/Recipes.Api/Endpoints/RecipesEndpoints.cs` | Add `POST /{id}/analyse-nutrition` |
| `Backend/src/Recipes.Infrastructure/DependencyInjection.cs` | Register `IMcpClientHost`, `INutritionAnalysisAgent` |
