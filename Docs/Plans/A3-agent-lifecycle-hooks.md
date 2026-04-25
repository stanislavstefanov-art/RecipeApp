# A3 — Agent Lifecycle Hooks: Implementation Plan

Reference spec: `Docs/specs/A3-agent-lifecycle-hooks.md`

Build order: types → runner → hook implementations → sub-agent wiring → DI → CCAF doc.

---

## Step 1 — IAgentHook.cs (Application/Common/AI)

`HookDecision` discriminated union (abstract record + two sealed record variants),
`AgentHookContext`, `ToolCallOutcome`, `IAgentHook` interface.

---

## Step 2 — IAgentHookRunner.cs (Application/Common/AI)

Single interface:
```csharp
Task<HookDecision> RunBeforeAsync(AgentHookContext ctx, CancellationToken ct);
Task RunAfterAsync(AgentHookContext ctx, ToolCallOutcome outcome, CancellationToken ct);
```

---

## Step 3 — AgentHookRunner.cs (Infrastructure/AI/Claude/Hooks)

Concrete runner: takes `IEnumerable<IAgentHook>`. `RunBeforeAsync` iterates hooks in
order; first Block short-circuits and is returned. `RunAfterAsync` always runs all hooks.

---

## Step 4 — LoggingAgentHook.cs (Infrastructure/AI/Claude/Hooks)

Stateless. `OnBeforeToolCallAsync` logs agent/tool/iteration at Debug level.
`OnAfterToolCallAsync` logs IsError/LatencyMs at Debug (Info when IsError).
Always returns `HookDecision.Allow()`.

---

## Step 5 — SlotCapacityHook.cs (Infrastructure/AI/Claude/Hooks)

Scoped (stateful per request). Tracks seen `(date, mealType)` slots via
`ConcurrentDictionary<string, byte>`. On `assign_meal`: parse date + mealType from
input; `TryAdd` → if already present return `Block(reason)`; else `Allow`.
`OnAfterToolCallAsync` is a no-op.

---

## Step 6 — Sub-agent wiring

**RecipeDiscoverySubAgent:**
- Add `IAgentHookRunner _hookRunner` field + constructor parameter.
- Add `int iteration` parameter to `InstrumentedDispatchAsync`.
- Inside: build `AgentHookContext`, call `RunBeforeAsync`; if Block → record
  telemetry(IsError=true) + return `ErrorResult`; else execute existing logic;
  then call `RunAfterAsync`.
- Update call site to pass `iteration`.

**MealAssignmentSubAgent:** same pattern (agent name = "MealAssignmentSubAgent").

---

## Step 7 — DI

```csharp
services.AddScoped<IAgentHook, LoggingAgentHook>();
services.AddScoped<IAgentHook, SlotCapacityHook>();
services.AddScoped<IAgentHookRunner, AgentHookRunner>();
```

---

## Step 8 — CCAF doc

Create `Backend/Docs/CCAF/A3-agent-lifecycle-hooks.md`.
