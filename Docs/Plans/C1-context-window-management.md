# C1 — Context Window Management: Implementation Plan

Reference spec: `Docs/specs/C1-context-window-management.md`

Build order: interface → implementation → ClaudeOptions additions →
agent modifications (3) → DI → CCAF doc.

---

## Step 1 — IContextWindowManager interface

Create `Backend/src/Recipes.Application/Common/AI/IContextWindowManager.cs`:

```csharp
using Recipes.Infrastructure.AI.Claude.Models;  // ← NOT here — interface is in Application

// Use the agent message type from the shared model namespace. Because
// ClaudeAgentMessage lives in Infrastructure, the interface should use
// a plain List<T> constraint with a shared type, OR live in Infrastructure.
// Decision: put the interface in Infrastructure.Common to avoid a
// cross-layer reference, since ClaudeAgentMessage is an Infrastructure type.
```

**Correction**: `ClaudeAgentMessage` is in `Recipes.Infrastructure`. The interface
should live in `Recipes.Infrastructure` to avoid a circular dependency.

Create `Backend/src/Recipes.Infrastructure/AI/IContextWindowManager.cs`:

```csharp
using Recipes.Infrastructure.AI.Claude.Models;

namespace Recipes.Infrastructure.AI;

public interface IContextWindowManager
{
    int Trim(List<ClaudeAgentMessage> messages, int maxMessages);
}
```

---

## Step 2 — ContextWindowManager implementation

Create `Backend/src/Recipes.Infrastructure/AI/ContextWindowManager.cs`:

```csharp
using Microsoft.Extensions.Logging;
using Recipes.Infrastructure.AI.Claude.Models;

namespace Recipes.Infrastructure.AI;

public sealed class ContextWindowManager : IContextWindowManager
{
    private readonly ILogger<ContextWindowManager> _logger;

    public ContextWindowManager(ILogger<ContextWindowManager> logger)
        => _logger = logger;

    public int Trim(List<ClaudeAgentMessage> messages, int maxMessages)
    {
        if (messages.Count <= maxMessages)
            return 0;

        // Keep messages[0] (original task) and the most recent WindowSize messages.
        var windowSize  = maxMessages / 2;
        var keepFromIdx = messages.Count - windowSize;
        var dropCount   = keepFromIdx - 1;   // how many from index 1 to drop

        if (dropCount <= 0)
            return 0;

        messages.RemoveRange(1, dropCount);

        _logger.LogWarning(
            "ContextWindowManager: dropped {Dropped} messages to stay within " +
            "MaxContextMessages={Max}. Remaining: {Remaining}.",
            dropCount, maxMessages, messages.Count);

        return dropCount;
    }
}
```

---

## Step 3 — ClaudeOptions additions

Modify `ClaudeOptions.cs`:

```csharp
public int MaxContextMessages { get; init; } = 20;
public int TokenBudgetWarningThreshold { get; init; } = 80_000;
```

---

## Step 4 — RecipeImportAgent changes

1. Add `IContextWindowManager _contextManager` field + constructor param.
2. At the top of the `for` loop, before `CallClaudeAsync`:
   ```csharp
   _contextManager.Trim(messages, _options.MaxContextMessages);
   ```
3. After reading `response`, add token budget check:
   ```csharp
   if ((response.Usage?.InputTokens ?? 0) > _options.TokenBudgetWarningThreshold)
       _logger.LogWarning(
           "RecipeImportAgent context approaching budget: {InputTokens}/{Threshold} tokens.",
           response.Usage!.InputTokens, _options.TokenBudgetWarningThreshold);
   ```

---

## Step 5 — RecipeDiscoverySubAgent changes

Same two changes as Step 4, scoped to `RecipeDiscoverySubAgent`.

---

## Step 6 — MealAssignmentSubAgent changes

Same two changes as Step 4, scoped to `MealAssignmentSubAgent`.

---

## Step 7 — DI registration

```csharp
services.AddScoped<IContextWindowManager, ContextWindowManager>();
```

---

## Step 8 — CCAF doc

Create `Backend/Docs/CCAF/C1-context-window-management.md` covering:
- What this implements
- CCAF subtopics table (5.1)
- Architecture diagram
- Key decisions (keep-first rule, WindowSize = max/2, token budget warning,
  why all three agents need it, scoped vs. singleton)
