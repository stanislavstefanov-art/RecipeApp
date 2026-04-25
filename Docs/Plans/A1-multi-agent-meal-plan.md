# A1 — Multi-Agent Meal Plan Orchestrator: Implementation Plan

Reference spec: `Docs/specs/A1-multi-agent-meal-plan.md`

Build order: models → tool definitions → sub-agents → orchestrator → application layer → endpoint → DI → CCAF doc.

---

## Step 1 — Context records and shared models

Create `Backend/src/Recipes.Infrastructure/AI/Claude/Agents/MealPlanAgentModels.cs`:

```csharp
namespace Recipes.Infrastructure.AI.Claude.Agents;

// Orchestrator decomposition output
record TaskDecomposition(string RecipeFocus, string DiversityGoal, int ServingsPerMeal);

// Orchestrator → RecipeDiscoverySubAgent
record RecipeDiscoveryContext(
    HouseholdPlanningProfileDto Household,
    TaskDecomposition Decomposition,
    int DaysCount,
    IReadOnlyList<int> MealTypes);

// Discovery output (also MealAssignmentSubAgent input ingredient)
record RecipeCandidate(Guid RecipeId, string RecipeName, string Suitability);

// Orchestrator → MealAssignmentSubAgent
record MealAssignmentContext(
    IReadOnlyList<RecipeCandidate> Candidates,
    HouseholdPlanningProfileDto Household,
    DateOnly StartDate,
    int DaysCount,
    IReadOnlyList<int> MealTypes);
```

Uses `HouseholdPlanningProfileDto` from `SuggestMealPlan` slice — no new domain types needed.

---

## Step 2 — Tool definitions

Create `Backend/src/Recipes.Infrastructure/AI/Claude/Agents/MealPlanAgentTools.cs` with
`ClaudeToolDefinition` constants for each tool using inline JSON schemas:

**Orchestrator tools (1):**
- `decompose_task(recipeFocus, diversityGoal, servingsPerMeal)` — terminal

**RecipeDiscoverySubAgent tools (4):**
- `list_available_recipes()` → `[{id, name}]`
- `get_recipe_details(recipeId)` → ingredients, servings
- `get_recent_meal_history(householdId, days)` → `[recipeId]`
- `submit_candidates(candidates: [{recipeId, recipeName, suitability}])` — terminal

**MealAssignmentSubAgent tools (2):**
- `assign_meal(date, mealType, recipeId, personIds: [])` — accumulator
- `finalize_assignments()` — terminal

---

## Step 3 — RecipeDiscoverySubAgent

Create `Backend/src/Recipes.Infrastructure/AI/Claude/Agents/RecipeDiscoverySubAgent.cs`:

- Constructor: `IHttpClientFactory`, `IRecipeRepository`, `IMealPlanRepository`,
  `IOptions<ClaudeOptions>`, `ILogger`, `IToolCallTelemetry`
- `RunAsync(RecipeDiscoveryContext ctx, CancellationToken ct) → Task<IReadOnlyList<RecipeCandidate>>`
- System prompt: focused on recipe selection — avoid repetition, match dietary preferences
- Initial user message includes serialised `ctx` (household profile + task guidance)
- Loop pattern identical to F2/F5 (max 10 iterations, `stop_reason: tool_use`)
- Tool dispatch:
  - `list_available_recipes` → `_recipeRepository.GetAllSummariesAsync()`
  - `get_recipe_details` → `_recipeRepository.GetByIdAsync()`
  - `get_recent_meal_history` → `_mealPlanRepository.GetRecentRecipeIdsAsync(householdId, days)`
  - `submit_candidates` → deserialise + set state.Complete
- Throws if loop exhausts without `submit_candidates`

---

## Step 4 — MealAssignmentSubAgent

Create `Backend/src/Recipes.Infrastructure/AI/Claude/Agents/MealAssignmentSubAgent.cs`:

- Constructor: `IHttpClientFactory`, `IOptions<ClaudeOptions>`, `ILogger`, `IToolCallTelemetry`
- `RunAsync(MealAssignmentContext ctx, CancellationToken ct) → Task<MealPlanSuggestionDto>`
- System prompt: assign each day × mealType exactly once; use `assign_meal` per entry;
  call `finalize_assignments` when all slots are filled
- Initial user message includes serialised candidates + week structure
- Tool dispatch:
  - `assign_meal(date, mealType, recipeId, personIds)` → appends to `state.Entries`
  - `finalize_assignments()` → validates all slots filled; builds `MealPlanSuggestionDto`
- Validation: days × mealTypes must all be assigned before `finalize_assignments` is accepted;
  if incomplete, return `ErrorResult` to force Claude to continue

---

## Step 5 — MealPlanOrchestratorAgent

Create `Backend/src/Recipes.Infrastructure/AI/Claude/Agents/MealPlanOrchestratorAgent.cs`:

- Constructor: `RecipeDiscoverySubAgent`, `MealAssignmentSubAgent`, `IHttpClientFactory`,
  `IHouseholdRepository`, `IPersonRepository`, `IOptions<ClaudeOptions>`, `ILogger`
- `RunAsync(SuggestMealPlanMultiAgentCommand cmd, CancellationToken ct) → Task<ErrorOr<MealPlanSuggestionDto>>`

**Phase 1 — Decompose:**
```csharp
// Single Claude call, tool_choice: { type: "tool", name: "decompose_task" }
var decomposition = await DecomposeAsync(cmd, household, ct);
```
Forces Claude to call `decompose_task` immediately (no multi-turn loop needed).

**Phase 2 — Discovery:**
```csharp
var context = new RecipeDiscoveryContext(householdProfile, decomposition, cmd.NumberOfDays, cmd.MealTypes);
var candidates = await _discoveryAgent.RunAsync(context, ct);
```

**Phase 3 — Assignment:**
```csharp
var assignCtx = new MealAssignmentContext(candidates, householdProfile, cmd.StartDate, cmd.NumberOfDays, cmd.MealTypes);
var plan = await _assignmentAgent.RunAsync(assignCtx, ct);
```

**Phase 4 — Return:**
```csharp
return plan;
```

The orchestrator's `DecomposeAsync` uses forced `tool_choice` (same pattern as F2's `save_recipe`) to guarantee a structured output on the first call without looping.

---

## Step 6 — Application layer

Create `Backend/src/Recipes.Application/MealPlans/SuggestMealPlanMultiAgent/`:
- `SuggestMealPlanMultiAgentCommand.cs` — same shape as `SuggestMealPlanCommand`; returns `ErrorOr<MealPlanSuggestionDto>`
- `IMealPlanOrchestratorAgent.cs` — `RunAsync(SuggestMealPlanMultiAgentCommand, CT) → Task<ErrorOr<MealPlanSuggestionDto>>`
- `SuggestMealPlanMultiAgentHandler.cs` — fetches household + builds command, delegates to `IMealPlanOrchestratorAgent`

---

## Step 7 — Endpoint

Add to `MealPlansEndpoints.cs`:
```csharp
group.MapPost("/suggest/multi-agent", async (SuggestMealPlanMultiAgentRequest req, ISender sender, CT ct) =>
{
    var cmd = new SuggestMealPlanMultiAgentCommand(...);
    var result = await sender.Send(cmd, ct);
    return result.ToHttpResult(dto => Results.Ok(dto));
});
```

Same request shape as `SuggestMealPlanRequest`.

---

## Step 8 — DI registration

In `DependencyInjection.cs` add:
```csharp
services.AddScoped<RecipeDiscoverySubAgent>();
services.AddScoped<MealAssignmentSubAgent>();
services.AddScoped<IMealPlanOrchestratorAgent, MealPlanOrchestratorAgent>();
```

Sub-agents registered as concrete types (not interfaces) since only the orchestrator uses them directly.

---

## Step 9 — CCAF doc

Create `Backend/Docs/CCAF/A1-multi-agent-meal-plan.md` covering subtopics 1.2, 1.3, 1.6
with code references to the three agents and the context record contracts.
