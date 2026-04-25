# F8 — Tool-Call Observability

## Summary

Adds a cross-cutting telemetry layer that records a structured entry for every
`tool_use` invocation produced by the agentic loops in F2 (`RecipeImportAgent`) and
F5 (`NutritionAnalysisAgent`). Records are stored in a singleton in-memory ring buffer
and exposed via a new admin endpoint `GET /api/admin/tool-calls?limit=N`.

No existing AI feature implementations are modified.

---

## CCAF subtopics covered

| Subtopic | Where |
|---|---|
| Tool-call observability | `IToolCallTelemetry` + `InMemoryToolCallTelemetry` — structured record per invocation |
| Token accounting | `ToolCallRecord.InputTokens/OutputTokens` sourced from `ClaudeUsage` on every turn |
| Cost awareness | `CacheReadTokens` / `CacheCreationTokens` — cached vs. uncached input distinction |
| Per-tool latency | `Stopwatch` in `InstrumentedDispatchAsync` — wall-clock time per tool call |
| Instrumentation without touching core logic | Wrapper delegates to unchanged `DispatchToolAsync` |

---

## Architecture

```
RecipeImportAgent / NutritionAnalysisAgent
  │
  ├── CallClaudeAsync() → ClaudeAgentResponse (with .Usage)
  │
  └── Task.WhenAll → InstrumentedDispatchAsync (new wrapper)
            ├── Stopwatch.StartNew()
            ├── DispatchToolAsync()  ← unchanged
            ├── sw.Stop()
            └── IToolCallTelemetry.Record(ToolCallRecord)
                        │
                        ▼
              InMemoryToolCallTelemetry (singleton, ConcurrentQueue, cap 1000)
                        │
                        ▼
              GET /api/admin/tool-calls?limit=N
```

---

## Files to create

| Path | Purpose |
|---|---|
| `Backend/src/Recipes.Application/Common/AI/IToolCallTelemetry.cs` | Interface + `ToolCallRecord` data type |
| `Backend/src/Recipes.Infrastructure/Telemetry/InMemoryToolCallTelemetry.cs` | Singleton in-memory ring buffer implementation |
| `Backend/src/Recipes.Application/Admin/GetToolCallsQuery.cs` | MediatR query + handler + `ToolCallDto` |
| `Backend/src/Recipes.Api/Endpoints/AdminEndpoints.cs` | `GET /api/admin/tool-calls?limit=N` minimal-API endpoint |
| `Backend/Docs/CCAF/F8-tool-observability.md` | CCAF documentation |

## Files to modify

| Path | Change |
|---|---|
| `Backend/src/Recipes.Infrastructure/AI/Claude/Agents/RecipeImportAgent.cs` | Inject `IToolCallTelemetry`; replace `DispatchToolAsync` call with `InstrumentedDispatchAsync` wrapper |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Agents/NutritionAnalysisAgent.cs` | Same |
| `Backend/src/Recipes.Infrastructure/DependencyInjection.cs` | Register `InMemoryToolCallTelemetry` as singleton |
| `Backend/src/Recipes.Api/Program.cs` | Wire `app.MapAdminEndpoints()` |
| `Backend/tests/Recipes.Application.Tests/Recipes/ImportRecipeFromUrl/RecipeImportAgentTests.cs` | Supply `NoOpTelemetry` stub to agent constructor |

---

## ToolCallRecord fields

| Field | Source |
|---|---|
| `AgentName` | Hardcoded in wrapper (`"RecipeImportAgent"` / `"NutritionAnalysisAgent"`) |
| `ToolName` | `toolUse.Name` |
| `InputBytes` | `toolUse.Input?.GetRawText().Length` |
| `OutputBytes` | `result.Content?.Length` |
| `LatencyMs` | `Stopwatch.ElapsedMilliseconds` |
| `IsError` | `result.IsError ?? false` |
| `InputTokens` / `OutputTokens` | `ClaudeUsage` from the turn that requested the tool |
| `CacheReadTokens` / `CacheCreationTokens` | `ClaudeUsage` nullable fields |

Token data is per Claude API call. When multiple tools are dispatched in parallel from
one turn, each record for that turn shares the same token counts.

---

## Acceptance criteria

1. `dotnet build Backend/Recipes.sln` passes.
2. `dotnet test` (unit tests) passes.
3. `GET /api/admin/tool-calls` returns `[]` on a fresh server start.
4. After triggering `POST /api/recipes/import/url`, the endpoint returns at least one
   record with a non-zero `LatencyMs` and matching `AgentName = "RecipeImportAgent"`.
5. Token fields are populated (non-null) when the Claude API key is valid.
6. Oldest records are evicted when the buffer exceeds 1,000 entries.
