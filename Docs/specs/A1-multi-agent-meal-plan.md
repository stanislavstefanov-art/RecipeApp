# A1 — Multi-Agent Meal Plan Orchestrator

## Summary

A new endpoint `POST /api/meal-plans/suggest/multi-agent` that replaces the single
Claude call in the existing `SuggestMealPlan` flow with an **orchestrator + two
sub-agents** pattern. The orchestrator first asks Claude to decompose the planning task,
then invokes a `RecipeDiscoverySubAgent` (find suitable recipes) and a
`MealAssignmentSubAgent` (assign recipes to days and members), passing structured
context between them.

The existing `POST /api/meal-plans/suggest` endpoint and `IMealPlanSuggestionService`
are untouched.

---

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| Multi-Agent Orchestration (1.2) | `MealPlanOrchestratorAgent` coordinates two specialised sub-agents; each has its own Claude conversation, system prompt, and tool set |
| Subagent Invocation & Context Passing (1.3) | Orchestrator invokes sub-agents via explicit typed context records (`RecipeDiscoveryContext`, `MealAssignmentContext`); sub-agent output is the input to the next |
| Task Decomposition Strategies (1.6) | First Claude call in orchestrator uses `decompose_task` tool to produce a `TaskDecomposition` before any sub-agent is invoked |

---

## Architecture

```
POST /api/meal-plans/suggest/multi-agent
          │  MediatR
          ▼
SuggestMealPlanMultiAgentHandler
          │
          ▼
IMealPlanOrchestratorAgent.RunAsync(command)
          │
          ├── Phase 1: Decompose (orchestrator Claude call)
          │   Tool: decompose_task → TaskDecomposition
          │   {recipeFocus, diversityGoal, servingsPerMeal}
          │
          ├── Phase 2: Recipe Discovery (sub-agent)
          │   RecipeDiscoverySubAgent.RunAsync(RecipeDiscoveryContext)
          │   Context in:  household profile + task decomposition
          │   Tools:       list_available_recipes, get_recipe_details,
          │                get_recent_meal_history, submit_candidates
          │   Context out: RecipeCandidate[]
          │
          ├── Phase 3: Meal Assignment (sub-agent)
          │   MealAssignmentSubAgent.RunAsync(MealAssignmentContext)
          │   Context in:  candidates + household members + week structure
          │   Tools:       assign_meal (accumulator), finalize_assignments
          │   Context out: MealPlanSuggestionDto
          │
          └── Return MealPlanSuggestionDto
```

---

## Context records (explicit handoff contracts)

```csharp
// Orchestrator → RecipeDiscoverySubAgent
record RecipeDiscoveryContext(
    HouseholdPlanningProfileDto Household,
    TaskDecomposition Decomposition,
    int DaysCount,
    IReadOnlyList<int> MealTypes);

// RecipeDiscoverySubAgent → MealAssignmentSubAgent (via orchestrator)
record RecipeCandidate(Guid RecipeId, string RecipeName, string Suitability);

// Orchestrator → MealAssignmentSubAgent
record MealAssignmentContext(
    IReadOnlyList<RecipeCandidate> Candidates,
    HouseholdPlanningProfileDto Household,
    DateOnly StartDate,
    int DaysCount,
    IReadOnlyList<int> MealTypes);
```

---

## Sub-agent tool sets

### RecipeDiscoverySubAgent

| Tool | Description |
|---|---|
| `list_available_recipes` | Returns all recipes (id + name + brief tags) |
| `get_recipe_details` | Full recipe: ingredients, steps, servings |
| `get_recent_meal_history` | Recipes used in last N days — for avoiding repetition |
| `submit_candidates` | **Terminal** — emits the candidate list and ends the loop |

### MealAssignmentSubAgent

| Tool | Description |
|---|---|
| `assign_meal` | Accumulates one entry: date, mealType, recipeId, personIds |
| `finalize_assignments` | **Terminal** — validates coverage (all days × all mealTypes) and ends the loop |

---

## Response

Returns the same `MealPlanSuggestionDto` shape as the existing endpoint so the frontend
and `POST /api/meal-plans/accept-suggestion` can consume it without changes.

---

## Files to create

| Path | Purpose |
|---|---|
| `Backend/src/Recipes.Application/MealPlans/SuggestMealPlanMultiAgent/SuggestMealPlanMultiAgentCommand.cs` | Command + handler interface |
| `Backend/src/Recipes.Application/MealPlans/SuggestMealPlanMultiAgent/IMealPlanOrchestratorAgent.cs` | Agent interface |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Agents/MealPlanOrchestratorAgent.cs` | Orchestrator implementation |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Agents/RecipeDiscoverySubAgent.cs` | Sub-agent 1 |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Agents/MealAssignmentSubAgent.cs` | Sub-agent 2 |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Agents/MealPlanAgentTools.cs` | Tool definitions for both sub-agents |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Agents/MealPlanAgentModels.cs` | Context records + DTO models |
| `Backend/Docs/CCAF/A1-multi-agent-meal-plan.md` | CCAF documentation |

## Files to modify

| Path | Change |
|---|---|
| `Backend/src/Recipes.Api/Endpoints/MealPlansEndpoints.cs` | Add `POST /api/meal-plans/suggest/multi-agent` |
| `Backend/src/Recipes.Infrastructure/DependencyInjection.cs` | Register orchestrator + sub-agents |

---

## Acceptance criteria

1. `dotnet build Backend/Recipes.sln` passes.
2. `POST /api/meal-plans/suggest/multi-agent` returns a valid `MealPlanSuggestionDto`
   with entries covering the requested days × meal types.
3. Logs show three distinct Claude interactions: orchestrator decompose call, discovery
   sub-agent loop, assignment sub-agent loop.
4. Context records are populated non-trivially: discovery sub-agent receives the
   orchestrator's `TaskDecomposition`; assignment sub-agent receives `RecipeCandidate[]`
   from the discovery sub-agent.
5. Existing `POST /api/meal-plans/suggest` is unchanged.
