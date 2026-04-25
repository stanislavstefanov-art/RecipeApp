# A3 — Agent Lifecycle Hooks

## What this implements

A formal hook system for the A1 multi-agent loop. `IAgentHook` defines two lifecycle
points in every tool dispatch: `OnBeforeToolCallAsync` (can block the call) and
`OnAfterToolCallAsync` (observational). `IAgentHookRunner` chains all registered hooks
in registration order and short-circuits on the first `Block` decision.

Two hook implementations ship: `LoggingAgentHook` emits structured log events before
and after every tool call; `SlotCapacityHook` (scoped, stateful) blocks a second
`assign_meal` call for the same day × mealType slot within a single workflow run.

---

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **Agent Lifecycle Hooks (1.5)** | `IAgentHook.OnBeforeToolCallAsync` fires before every `DispatchToolAsync` call in `RecipeDiscoverySubAgent` and `MealAssignmentSubAgent`; a `HookDecision.BlockDecision` return skips dispatch entirely and injects an `is_error: true` result into the Claude conversation, forcing the model to choose a different action |

---

## Architecture

```
InstrumentedDispatchAsync(toolUse, ..., iteration, ct)
        │
        ├── _hookRunner.RunBeforeAsync(AgentHookContext)
        │       ├── LoggingAgentHook   → logs before event, returns Allow
        │       └── SlotCapacityHook   → checks assign_meal slot; Allow or Block
        │
        │   if Block ──▶ record telemetry(IsError=true)
        │                return ErrorResult(id, block.Reason)
        │                (Claude sees is_error: true and must choose a different slot)
        │
        ├── DispatchToolAsync          ← only reached if all hooks Allow
        │
        ├── record telemetry
        │
        └── _hookRunner.RunAfterAsync(AgentHookContext, ToolCallOutcome)
                └── LoggingAgentHook  logs after event with latency and IsError
```

---

## Key decisions

### HookDecision as a discriminated union
`HookDecision` is an `abstract record` with `AllowDecision` and `BlockDecision`
variants, not a bool or enum. Pattern matching on the return value (`if (decision is
HookDecision.BlockDecision block)`) makes the block reason available at the call site
without a separate out-parameter.

### Scoped SlotCapacityHook for request-scoped state
`SlotCapacityHook` is registered as `AddScoped` so it lives for the duration of one
HTTP request. Both sub-agents within the same workflow run share the same instance —
the assignment hook tracks slots across the full multi-agent execution, not per sub-agent.

### Hook runner short-circuits on first Block
`AgentHookRunner.RunBeforeAsync` returns the first `BlockDecision` immediately without
running subsequent hooks. This is the same semantic as ASP.NET Core middleware: a
blocking hook is authoritative and later hooks should not override it.

### Telemetry recorded even for blocked calls
When a hook blocks a tool call, `InstrumentedDispatchAsync` still records a
`ToolCallRecord` with `IsError = true` before returning. Blocked calls appear in the
`GET /api/admin/tool-calls` telemetry feed, making hook behaviour observable.

### Iteration counter passed to hook context
The `iteration` loop counter from the outer `for` loop is threaded down to
`InstrumentedDispatchAsync` and included in `AgentHookContext.Iteration`. This lets
hooks apply policies based on how far into the loop a call occurs (e.g., escalate
log level on late iterations).
