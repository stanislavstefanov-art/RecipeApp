# C1 — Context Window Management

## Summary

All three agentic loops (`RecipeImportAgent`, `RecipeDiscoverySubAgent`,
`MealAssignmentSubAgent`) accumulate the full message history without any
truncation. After 10–15 iterations each turn appends an `assistant` message
and a `user` message containing tool results; large tool outputs (e.g.
`list_available_recipes` returning hundreds of recipes as JSON) can push
`InputTokens` into the hundreds of thousands, silently degrading output quality
or hitting API limits.

This feature adds a `ContextWindowManager` that applies a **keep-first /
sliding-window** trim to the message list before every Claude call. The trim
always keeps the initial user message (it contains the task description),
removes the oldest middle messages when the list exceeds `MaxContextMessages`,
and logs a structured warning whenever messages are dropped or when
`InputTokens` reported by Claude exceeds a configurable budget threshold.

No existing endpoints are modified.

---

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **5.1 — Context Window Management** | `ContextWindowManager.Trim(messages, max)` applies a keep-first / sliding-window strategy before every Claude call in all three agentic loops; a `TokenBudgetWarningThreshold` in `ClaudeOptions` triggers a structured log warning when `InputTokens` exceeds it — making the agents explicitly context-window-aware instead of silently accumulating unbounded history |

---

## Architecture

```
RecipeImportAgent / RecipeDiscoverySubAgent / MealAssignmentSubAgent
        │
        │  for each iteration:
        │    1. IContextWindowManager.Trim(messages, MaxContextMessages)
        │         if messages.Count > max:
        │           keep messages[0]   (original task — never dropped)
        │           drop messages[1 .. count - WindowSize]
        │           keep messages[count - WindowSize .. count]   (recent turns)
        │         log Warning: "Trimmed N messages to stay within context window"
        │
        │    2. CallClaudeAsync(messages, tools)
        │
        │    3. if response.Usage.InputTokens > TokenBudgetWarningThreshold:
        │         log Warning: "Context approaching budget: {InputTokens}/{Threshold}"
        │
        └── agent continues with trimmed messages
```

**Keep-first / sliding-window rule:**

| Input count | Action |
|---|---|
| ≤ `MaxContextMessages` | No-op |
| > `MaxContextMessages` | Drop `messages[1 .. count - WindowSize]`; keep `messages[0]` + last `WindowSize` |

`WindowSize = MaxContextMessages / 2` (i.e. the most recent half is always kept).

---

## Configuration additions to ClaudeOptions

```csharp
// Maximum messages in agent loop history before trimming (default: 20)
public int MaxContextMessages { get; init; } = 20;

// InputTokens threshold above which a budget warning is logged (default: 80000)
public int TokenBudgetWarningThreshold { get; init; } = 80_000;
```

---

## IContextWindowManager interface

```csharp
public interface IContextWindowManager
{
    // Trims messages in-place using keep-first / sliding-window.
    // Returns the number of messages dropped (0 if no trim needed).
    int Trim(List<ClaudeAgentMessage> messages, int maxMessages);
}
```

---

## Files to create

| Path | Purpose |
|---|---|
| `Backend/src/Recipes.Application/Common/AI/IContextWindowManager.cs` | Interface |
| `Backend/src/Recipes.Infrastructure/AI/ContextWindowManager.cs` | Implementation |
| `Backend/Docs/CCAF/C1-context-window-management.md` | CCAF documentation |

## Files to modify

| Path | Change |
|---|---|
| `Backend/src/Recipes.Infrastructure/Options/ClaudeOptions.cs` | Add `MaxContextMessages` and `TokenBudgetWarningThreshold` |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Agents/RecipeImportAgent.cs` | Inject `IContextWindowManager`; call `Trim` before each Claude call; log token budget warning |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Agents/RecipeDiscoverySubAgent.cs` | Same |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Agents/MealAssignmentSubAgent.cs` | Same |
| `Backend/src/Recipes.Infrastructure/DependencyInjection.cs` | Register `IContextWindowManager` (scoped) |

---

## Acceptance criteria

1. `dotnet build Backend/Recipes.sln` passes.
2. When a simulated message list of 25 entries is passed to `ContextWindowManager.Trim`
   with `maxMessages = 20`, the list is trimmed to 20: `messages[0]` is preserved,
   `messages[1..4]` are dropped, and `messages[5..24]` are kept.
3. Trim is a no-op when `messages.Count ≤ maxMessages`.
4. `TokenBudgetWarningThreshold` is wired into each agent's iteration body — a log
   Warning appears when `InputTokens` exceeds it.
5. Existing tests in `Recipes.Application.Tests` still pass (51 tests).
