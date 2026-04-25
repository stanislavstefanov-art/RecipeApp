# F5 — MCP Client Consumption (nutrition lookup)

## What this implements

A new endpoint `POST /api/recipes/{id}/analyse-nutrition` that runs a Claude agent backed
by two tool sources: a static local tool (`get_recipe_ingredients`) and an MCP-proxied
tool (`lookup_nutrition`) sourced at runtime from a standalone `NutritionServer` MCP
server. The agent loops until Claude calls `save_nutrition_analysis`, then returns a
per-ingredient nutrition summary with totals.

This demonstrates the full MCP client-consumption flow: launching an MCP server as a
subprocess, listing its tools, converting them to `ClaudeToolDefinition` records, and
dispatching tool calls through the MCP client when Claude invokes them.

---

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| MCP client consumption | `NutritionMcpClient.cs` — `McpClient.CreateAsync(StdioClientTransport)`, `ListToolsAsync`, `CallToolAsync` |
| Proxying MCP tools into Claude's `tools` array | `NutritionAnalysisAgent.BuildToolList()` — converts `McpToolDefinition` → `ClaudeToolDefinition` and merges with static tools |
| Composing multiple tool sources | Agent's tool list = 2 static tools + N MCP tools assembled at runtime; dispatch switches on tool name |
| MCP stdio subprocess transport | `StdioClientTransport` with `Command: "dotnet run ..."` launches the nutrition server as a child process |
| Parallel tool calls | `Task.WhenAll(toolUseBlocks.Select(DispatchToolAsync))` — all `lookup_nutrition` calls in one Claude turn run concurrently |
| Tool error handling (`is_error: true`) | `ExecuteMcpToolAsync` catches exceptions and returns `is_error: true` content blocks to Claude |

---

## Key decisions

### `McpClient.CreateAsync` with lazy initialisation
`NutritionMcpClient` wraps `McpClient` in a `Lazy<Task<McpClient>>` so the subprocess
is started on the first real call (not at DI resolution time). This avoids spawning a
`dotnet run` process for every request that never actually calls the nutrition endpoint.

### Runtime tool list composition
`NutritionAnalysisAgent.BuildToolList()` calls `IMcpClientHost.ListToolsAsync()` before
each `RunAsync` invocation and merges the returned `McpToolDefinition` records with the
two static tool definitions. Claude sees a unified `tools` array; it has no knowledge of
which tools are local vs. remote. The dispatcher then routes by tool name:
- `get_recipe_ingredients` / `save_nutrition_analysis` → local handlers
- anything else → `IMcpClientHost.CallToolAsync(...)` (MCP proxy)

This is the defining pattern for MCP client consumption: the AI consumer is oblivious to
tool provenance; only the dispatcher knows.

### Stub nutrition data is deterministic
`NutritionTools.LookupNutrition` derives values from `ingredient.GetHashCode()`, making
results consistent across calls for the same ingredient. This makes the demo repeatable
without external API keys or a real nutrition database.

### `IMcpClientHost : IAsyncDisposable`
The interface exposes `DisposeAsync()` so the DI container can shut down the subprocess
cleanly when the scoped lifetime ends. Without this, the `dotnet run` child process would
linger after the HTTP request completes.

---

## Comparison: static tools vs. MCP-proxied tools

| Concern | Static tools (F2, F5 local) | MCP-proxied tools (F5 `lookup_nutrition`) |
|---|---|---|
| Schema source | Hard-coded `JsonDocument.Parse(...)` in tool class | Retrieved live from MCP server via `ListToolsAsync` |
| Execution | C# method call in-process | `McpClient.CallToolAsync` → stdio → child process |
| Versioning | Recompile the backend | Replace/update the MCP server independently |
| Discoverability | Only via the agent that uses it | Any MCP-compatible host can discover it |
| Error handling | Exception-based | `CallToolResult.IsError` flag + exception on propagation |
