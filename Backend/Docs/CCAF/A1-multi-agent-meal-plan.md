# A1 — Multi-Agent Meal Plan Orchestrator

## What this implements

A new endpoint `POST /api/meal-plans/suggest/multi-agent` that uses an
**orchestrator + two sub-agents** pattern to generate a weekly meal plan. Each agent
runs as a separate Claude conversation with its own system prompt, tool set, and
loop lifecycle. Structured context records carry data between agents.

The existing `POST /api/meal-plans/suggest` and `IMealPlanSuggestionService` are
untouched.

---

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **Multi-Agent Orchestration (1.2)** | `MealPlanOrchestratorAgent` coordinates `RecipeDiscoverySubAgent` and `MealAssignmentSubAgent`; each is a separate Claude conversation with its own system prompt and tool set |
| **Subagent Invocation & Context Passing (1.3)** | Orchestrator passes typed `RecipeDiscoveryContext` to discovery sub-agent; discovery output (`RecipeCandidate[]`) is passed as `MealAssignmentContext` to assignment sub-agent |
| **Task Decomposition Strategies (1.6)** | Phase 1 is a forced-tool Claude call that produces `TaskDecomposition` before any sub-agent runs; decomposition steers the discovery prompt |

---

## Architecture

```
POST /api/meal-plans/suggest/multi-agent
          │  MediatR
          ▼
SuggestMealPlanMultiAgentHandler  (builds HouseholdPlanningProfileDto)
          │
          ▼
MealPlanOrchestratorAgent.RunAsync()
          │
          ├── Phase 1: DecomposeAsync()
          │   Single Claude call, tool_choice: forced "decompose_task"
          │   Input:  household profile + week params
          │   Output: TaskDecomposition {recipeFocus, diversityGoal, servingsPerMeal}
          │
          ├── Phase 2: RecipeDiscoverySubAgent.RunAsync(RecipeDiscoveryContext)
          │   Input context:  household + decomposition + week structure
          │   Tools:          list_available_recipes, get_recipe_details,
          │                   get_recent_meal_history, submit_candidates (terminal)
          │   Output context: RecipeCandidate[]
          │
          └── Phase 3: MealAssignmentSubAgent.RunAsync(MealAssignmentContext)
              Input context:  candidates + household + week structure
              Tools:          assign_meal (accumulator), finalize_assignments (terminal)
              Output:         MealPlanSuggestionDto
```

---

## Context records (handoff contracts)

The explicit typed context records are the defining pattern for subtopic 1.3.
They prevent implicit coupling between agents and make the data flowing between
them inspectable.

```
Orchestrator                      RecipeDiscoverySubAgent
     │── RecipeDiscoveryContext ───────────────────────────▶ RunAsync()
     │     {Household, Decomposition, DaysCount, MealTypes}        │
     │                                                             │
     │◀────────────────────── RecipeCandidate[] ─────────────────╯
     │
     │── MealAssignmentContext ──────────── MealAssignmentSubAgent
           {Candidates, Household,              RunAsync()
            StartDate, DaysCount, MealTypes}
```

---

## Task Decomposition (Phase 1)

Phase 1 uses `ClaudeToolChoiceForced("decompose_task")` — the same forced-tool pattern
from F2 — to guarantee a structured `TaskDecomposition` in a single API call without
a loop:

```csharp
ToolChoice: new ClaudeToolChoiceForced("decompose_task")
```

The decomposition's `RecipeFocus` is embedded in the discovery sub-agent's user
message, connecting the orchestrator's planning decision to the sub-agent's execution.
This is the task decomposition pattern: Claude at the orchestrator level decides *how*
to approach the task; sub-agents execute those decisions.

---

## Workflow enforcement in the assignment sub-agent

`MealAssignmentSubAgent.ExecuteFinalize` validates that every day × mealType slot is
covered before accepting `finalize_assignments`. If any slots are missing it returns an
`is_error: true` result naming the gaps, which forces Claude to continue assigning:

```csharp
var missing = expected.Except(actual).ToList();
if (missing.Count > 0)
    return ErrorResult(toolUseId, $"Incomplete plan — missing {missing.Count} slot(s)...");
state.Complete = true;
```

This is a precursor to the full workflow enforcement pattern in A2. Even without a
separate `WorkflowEnforcer`, the assignment sub-agent enforces its own completion
invariant through tool error feedback.

---

## Key decisions

### Sub-agents as concrete DI types, not interfaces
`RecipeDiscoverySubAgent` and `MealAssignmentSubAgent` are injected as concrete types
into the orchestrator because only the orchestrator calls them. Defining interfaces for
them would add abstraction with no benefit — they are implementation details of the
orchestrator, not public contracts.

### Forced tool_choice for decomposition
The orchestrator's Phase 1 uses forced `tool_choice` to guarantee structured output
in a single API call. A free-form Claude response would require parsing; a loop would
add latency for a deterministic one-shot call. Forced tool_choice is the correct pattern
when the required response structure is fully known in advance.

### Three distinct log entries per request
The orchestrator emits a log line at the start of each phase, making it easy to trace
the multi-agent flow in Application Insights: one log per agent invoked, with the
context size visible in the token counts from F8 telemetry.

### Same `MealPlanSuggestionDto` shape
Returning the same DTO as the existing suggest endpoint means `POST /accept-suggestion`
works unchanged, and the Angular/React frontends don't need modification.
