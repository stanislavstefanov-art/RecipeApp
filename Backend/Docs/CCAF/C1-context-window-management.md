# C1 — Context Window Management

## What this implements

All three agentic loops (`RecipeImportAgent`, `RecipeDiscoverySubAgent`,
`MealAssignmentSubAgent`) previously accumulated the full message history over
up to 10–15 iterations without any truncation. Tool results such as
`list_available_recipes` or `get_recipe_details` can be several kilobytes each;
over many iterations the accumulated `InputTokens` can grow into hundreds of
thousands, silently degrading output quality or hitting API context limits.

This feature adds `ContextWindowManager`, which applies a **keep-first /
sliding-window** trim to the message list before every Claude call in all three
agents. A `TokenBudgetWarningThreshold` in `ClaudeOptions` triggers a structured
`LogWarning` when `InputTokens` reported by Claude exceeds the threshold —
making the agents explicitly aware of their context footprint.

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **5.1 — Context Window Management** | `IContextWindowManager.Trim(messages, max)` is called at the top of each agent iteration loop, applying keep-first / sliding-window truncation when the message list exceeds `ClaudeOptions.MaxContextMessages`; `TokenBudgetWarningThreshold` comparison after each `CallClaudeAsync` response emits a `LogWarning` when `InputTokens` approaches the budget — wired into all three agents |

## Architecture

```
RecipeImportAgent / RecipeDiscoverySubAgent / MealAssignmentSubAgent
  for each iteration:
    1. IContextWindowManager.Trim(messages, MaxContextMessages)
       ├── messages.Count ≤ max  → no-op, return 0
       └── messages.Count > max:
             windowSize  = max / 2
             keepFromIdx = count - windowSize
             dropCount   = keepFromIdx - 1
             messages.RemoveRange(1, dropCount)
             LogWarning("dropped {N} messages")
             return dropCount

    2. CallClaudeAsync(messages, tools)

    3. if InputTokens > TokenBudgetWarningThreshold:
         LogWarning("context approaching budget: {X}/{Y}")
```

## Key decisions

**Keep-first rule** — `messages[0]` (the original task description) is always
preserved. It contains the household profile, URL, or assignment context that
the agent needs throughout. Dropping it would cause the agent to lose its
task definition.

**Sliding-window = max/2 recent messages** — keeping the most recent half
preserves the tool-use ↔ tool-result pairs that Claude needs to continue the
loop correctly. Cutting the oldest middle messages removes stale context
without breaking the current turn's chain-of-thought.

**Token budget warning instead of hard stop** — the warning is a structured
log event that can be surfaced in Application Insights without interrupting
the agent. A hard stop at a token limit would require a fallback plan; the
warning approach lets operators tune thresholds before escalating to a
hard limit.

**Scoped registration** — agents are scoped, so `ContextWindowManager` is
also scoped. Each request gets a fresh instance. (The manager itself is
stateless, so singleton would work too, but scoped is the safer default
to match the agents.)

**`MaxContextMessages = 20` default** — with `MaxIterations = 15` and two
messages per turn (assistant + user), the theoretical maximum is 31 messages
(1 initial + 15×2). A `MaxContextMessages` of 20 trims the oldest 10 messages
(keep first + last 10) when the loop approaches this ceiling, keeping the
message list manageable without affecting short loops.
