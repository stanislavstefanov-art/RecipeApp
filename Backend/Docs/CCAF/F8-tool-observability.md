# F8 — Tool-Call Observability

## What this implements

A cross-cutting telemetry layer that captures a structured record for every `tool_use`
invocation produced by the agentic loops in F2 (`RecipeImportAgent`) and F5
(`NutritionAnalysisAgent`). Records are stored in a singleton in-memory ring buffer and
exposed via a new admin endpoint `GET /api/admin/tool-calls?limit=N`.

No existing AI feature implementations are modified.

---

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| Tool-call observability | `IToolCallTelemetry` + `InMemoryToolCallTelemetry` — structured record per tool invocation |
| Token accounting | `ToolCallRecord.InputTokens/OutputTokens` — sourced from `ClaudeUsage` on every Claude API turn |
| Cost awareness | `CacheReadTokens` / `CacheCreationTokens` — distinguishes cached vs. uncached input |
| Per-tool latency | `Stopwatch` in `InstrumentedDispatchAsync` — wall-clock per tool call |
| Instrumentation without modifying core logic | `InstrumentedDispatchAsync` wrapper delegates to unchanged `DispatchToolAsync` |
| Input/output size tracking | `InputBytes` (JSON size of tool arguments), `OutputBytes` (size of tool result content) |
| Admin observability endpoint | `GET /api/admin/tool-calls?limit=N` — returns recent N records as JSON |

---

## Architecture

```
RecipeImportAgent.RunAsync()
  │
  ├── CallClaudeAsync() → ClaudeAgentResponse (has .Usage)
  │
  └── Task.WhenAll(toolUseBlocks.Select(b =>
          InstrumentedDispatchAsync(b, state, response.Usage, ct)))
                │
                ├── Stopwatch.StartNew()
                ├── DispatchToolAsync(b, state, ct)   ← unchanged
                ├── sw.Stop()
                └── _telemetry.Record(new ToolCallRecord { ... })
```

The same pattern is applied in `NutritionAnalysisAgent`. The `IToolCallTelemetry`
singleton accumulates records across both agents and all concurrent requests.

---

## ToolCallRecord fields

| Field | Source | Notes |
|---|---|---|
| `Id` | `Guid.NewGuid()` | Unique per invocation |
| `Timestamp` | `DateTime.UtcNow` | When the dispatch started |
| `AgentName` | Hardcoded in wrapper | `"RecipeImportAgent"` or `"NutritionAnalysisAgent"` |
| `ToolName` | `toolUse.Name` | e.g. `"normalize_ingredient"`, `"lookup_nutrition"` |
| `InputBytes` | `toolUse.Input?.GetRawText().Length` | JSON byte size of Claude's tool arguments |
| `OutputBytes` | `result.Content?.Length` | Byte size of the tool result string |
| `LatencyMs` | `Stopwatch.ElapsedMilliseconds` | Includes network for MCP tools, in-process for local tools |
| `IsError` | `result.IsError ?? false` | True if the tool returned an `is_error` content block |
| `InputTokens` | `usage?.InputTokens` | From the Claude turn that requested this tool |
| `OutputTokens` | `usage?.OutputTokens` | From the same Claude turn |
| `CacheReadTokens` | `usage?.CacheReadInputTokens` | Null when no prompt cache hit |
| `CacheCreationTokens` | `usage?.CacheCreationInputTokens` | Null when cache not being written |

**Token data granularity:** Claude's `usage` is per API call, not per tool. When multiple
tools are called in one turn (parallel tool use), each record for that turn shares the
same token counts. This is the correct level of attribution — parallel tools are served
by one Claude response.

---

## Storage

`InMemoryToolCallTelemetry` uses a `ConcurrentQueue<ToolCallRecord>` bounded at 1,000
entries (ring buffer: oldest evicted on overflow). Registered as a **singleton** so
telemetry persists across scoped requests.

In-memory is appropriate here because:
- No DB schema changes needed — F8 is a dev/observability aid, not a business feature.
- Data is useful only for the current server process lifetime.
- Azure Application Insights (production telemetry) would replace this for real
  deployments; `IToolCallTelemetry` can be re-implemented against App Insights without
  changing the agents.

---

## Key design decisions

### `InstrumentedDispatchAsync` wrapper, not a decorator
A decorator over `IRecipeImportAgent` / `INutritionAnalysisAgent` can only observe the
outer `RunAsync()` call — it cannot see individual tool dispatches inside the loop. The
wrapper method placed *inside* each agent, delegating to the existing `DispatchToolAsync`,
is the minimal invasive change that gives per-tool granularity.

### Token data on every tool record, not a separate turn record
The CCAF domain asks for "token accounting" at tool-call level. Attaching `ClaudeUsage`
fields to every `ToolCallRecord` (even though they repeat for parallel tools in the same
turn) means a single query to `GET /api/admin/tool-calls` returns a complete picture
without a join — simpler to consume and to understand.

### `IToolCallTelemetry` in Application, not Infrastructure
The interface and `ToolCallRecord` live in `Recipes.Application.Common.AI` so:
- Application-layer query handlers (`GetToolCallsHandler`) can reference the interface
  without depending on Infrastructure.
- Future implementations (e.g., Azure App Insights) added in Infrastructure satisfy the
  same contract.

This preserves the dependency direction: `Api → Application ← Infrastructure`.

---

## API contract

```
GET /api/admin/tool-calls?limit=100
```

Response: JSON array of `ToolCallDto` records, most-recent N entries, descending order
from ring buffer.

Example record:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "timestamp": "2026-04-25T10:42:01.123Z",
  "agentName": "RecipeImportAgent",
  "toolName": "normalize_ingredient",
  "inputBytes": 47,
  "outputBytes": 62,
  "latencyMs": 3,
  "isError": false,
  "inputTokens": 812,
  "outputTokens": 145,
  "cacheReadTokens": 780,
  "cacheCreationTokens": null
}
```
