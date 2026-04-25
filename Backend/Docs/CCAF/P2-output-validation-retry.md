# P2 — Output Validation and Retry

## What this implements

`POST /api/recipes/{id}/scale` accepts `{ fromServings, toServings }` and returns a
`ScaledRecipeDto` with adjusted ingredient quantities. `ClaudeRecipeScalingService`
implements a multi-turn validation-retry loop: if Claude's JSON response fails
schema validation, the error details are appended as a new user message in the same
conversation and Claude is asked to correct and resubmit. Up to three attempts are made.

---

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **Output Validation and Retry (4.3)** | `TryParseResponse` validates each attempt against explicit rules (root is an object, `ingredients` is a non-empty array, every item has a non-empty `name`, a positive `quantity`, and a `unit`). On failure, `ScaleAsync` appends the assistant's raw response and a `"user"` message listing each specific error, then re-calls the API — Claude sees both its own previous output and the errors it must fix |

---

## Architecture

```
POST /api/recipes/{id}/scale
        │  ScaleRecipeHandler
        │  ├── IRecipeRepository.GetByIdAsync → Recipe
        │  ├── map to RecipeDto
        │  └── IRecipeScalingService.ScaleAsync(dto, fromServings, toServings)
        │
        └── ClaudeRecipeScalingService  (up to 3 turns)
             │
             │  Turn 1 — messages: [user: recipe + scaling instruction]
             │  └── Claude → rawJson
             │         ├── TryParseResponse → valid  →  return ScaledRecipeDto (attempt=1)
             │         └── TryParseResponse → errors
             │
             │  Turn 2 — messages: [user, assistant: rawJson, user: error feedback]
             │  └── Claude → rawJson
             │         ├── TryParseResponse → valid  →  return ScaledRecipeDto (attempt=2)
             │         └── TryParseResponse → errors
             │
             │  Turn 3 — messages: [user, assistant, user, assistant, user: error feedback]
             │  └── Claude → rawJson
             │         ├── TryParseResponse → valid  →  return ScaledRecipeDto (attempt=3)
             │         └── throw InvalidOperationException (max attempts exceeded)
```

---

## Key decisions

### Multi-turn feedback, not prompt-level retry

Earlier features retry by re-calling the AI with the original prompt from scratch —
each attempt is independent. `ClaudeRecipeScalingService` instead **extends the
conversation**: it appends `{ role: "assistant", content: rawJson }` and
`{ role: "user", content: "Validation errors: ...\nPlease correct and resubmit." }`.
This gives Claude the exact error list alongside its own previous response, allowing
it to make targeted corrections rather than regenerating from scratch.

### Validation in the service layer, not the handler

`TryParseResponse` checks structural constraints (object root, non-empty array,
positive quantities) rather than domain constraints (correct scaling factor). Structural
correctness is the AI's responsibility; domain correctness (scaling accuracy) is
out of scope for a schema-validation loop.

### `AttemptsTaken` in the response

`ScaledRecipeDto.AttemptsTaken` records how many turns were needed. This makes the
retry behaviour observable without additional logging — a caller that receives
`AttemptsTaken: 2` knows a single correction turn was needed.

### System prompt is a constant, not a PromptBuilder call

Scaling is a narrow, deterministic task with no few-shot examples needed. A constant
`SystemPrompt` keeps the service focused on demonstrating the retry loop rather than
repeating P1's prompt-composition pattern.
