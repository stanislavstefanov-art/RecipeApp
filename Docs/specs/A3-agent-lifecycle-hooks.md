# A3 — Agent Lifecycle Hooks

## Summary

Adds a formal hook system to the A1 multi-agent loop. `IAgentHook` defines two
lifecycle points — `OnBeforeToolCallAsync` (can block) and `OnAfterToolCallAsync`
(observational). `IAgentHookRunner` chains all registered hooks. Two implementations
ship: a structured logging hook and a slot-capacity hook that blocks duplicate
`assign_meal` calls for the same day × mealType slot.

No existing endpoints are modified.

---

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **Agent Lifecycle Hooks (1.5)** | `IAgentHook.OnBeforeToolCallAsync` fires before every tool dispatch in the A1 sub-agents; a `HookDecision.Block(reason)` return short-circuits dispatch and injects an `is_error: true` result into the conversation, forcing Claude to recover |

---

## Architecture

```
MealAssignmentSubAgent / RecipeDiscoverySubAgent
        │
        ▼
InstrumentedDispatchAsync(toolUse, ..., iteration, ct)
        │
        ├── _hookRunner.RunBeforeAsync(AgentHookContext)
        │     ├── LoggingAgentHook  → Allow
        │     └── SlotCapacityHook  → Allow | Block(reason)
        │
        │   if Block → record telemetry(IsError=true), return ErrorResult
        │              (Claude sees is_error: true and must choose a different action)
        │
        ├── DispatchToolAsync  (existing tool execution)
        │
        ├── record telemetry
        │
        └── _hookRunner.RunAfterAsync(AgentHookContext, ToolCallOutcome)
              └── LoggingAgentHook logs structured after-event
```

---

## Types

```csharp
// HookDecision — discriminated union
abstract record HookDecision
{
    public static HookDecision Allow();
    public static HookDecision Block(string reason);
    sealed record AllowDecision : HookDecision;
    sealed record BlockDecision(string Reason) : HookDecision;
}

record AgentHookContext(string AgentName, string ToolName, JsonElement? Input, int Iteration);
record ToolCallOutcome(bool IsError, string? Content, long LatencyMs);

interface IAgentHook
{
    Task<HookDecision> OnBeforeToolCallAsync(AgentHookContext ctx, CancellationToken ct);
    Task OnAfterToolCallAsync(AgentHookContext ctx, ToolCallOutcome outcome, CancellationToken ct);
}

interface IAgentHookRunner
{
    Task<HookDecision> RunBeforeAsync(AgentHookContext ctx, CancellationToken ct);
    Task RunAfterAsync(AgentHookContext ctx, ToolCallOutcome outcome, CancellationToken ct);
}
```

---

## Hook implementations

| Hook | Type | Behaviour |
|---|---|---|
| `LoggingAgentHook` | `IAgentHook` (stateless) | Logs structured `AgentHookContext` before and `ToolCallOutcome` after each tool call |
| `SlotCapacityHook` | `IAgentHook` (scoped, stateful) | Tracks `assign_meal` calls per (date, mealType); blocks a second call for the same slot |

`SlotCapacityHook` is scoped so its state spans the entire HTTP request (both sub-agents share the same instance within one workflow run).

---

## Files to create

| Path | Purpose |
|---|---|
| `Backend/src/Recipes.Application/Common/AI/IAgentHook.cs` | `IAgentHook`, `HookDecision`, `AgentHookContext`, `ToolCallOutcome` |
| `Backend/src/Recipes.Application/Common/AI/IAgentHookRunner.cs` | `IAgentHookRunner` interface |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Hooks/AgentHookRunner.cs` | Runner implementation |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Hooks/LoggingAgentHook.cs` | Logging hook |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Hooks/SlotCapacityHook.cs` | Duplicate-slot blocking hook |
| `Backend/Docs/CCAF/A3-agent-lifecycle-hooks.md` | CCAF documentation |

## Files to modify

| Path | Change |
|---|---|
| `Backend/src/Recipes.Infrastructure/AI/Claude/Agents/RecipeDiscoverySubAgent.cs` | Inject `IAgentHookRunner`; add iteration param + hook calls to `InstrumentedDispatchAsync` |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Agents/MealAssignmentSubAgent.cs` | Same |
| `Backend/src/Recipes.Infrastructure/DependencyInjection.cs` | Register runner + hooks |

---

## Acceptance criteria

1. `dotnet build Backend/Recipes.sln` passes.
2. `LoggingAgentHook.OnBeforeToolCallAsync` is called for every tool dispatch in the A1 sub-agents.
3. `SlotCapacityHook.OnBeforeToolCallAsync` blocks a second `assign_meal` for the same slot, returning an `is_error: true` result to Claude.
4. Existing A1 orchestrator behaviour is unchanged when no slots are duplicated.
