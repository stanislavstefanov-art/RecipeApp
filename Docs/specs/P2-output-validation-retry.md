# P2 — Output Validation and Retry Loop (Recipe Scaling)

## Summary

Adds a `POST /api/recipes/{id}/scale` endpoint that asks Claude to scale a recipe's
ingredient quantities to a target serving count. After each Claude response the service
validates the JSON against explicit structural rules. On failure it appends the
assistant's raw output and a user message listing each specific error to the conversation,
then retries. Up to three attempts. `AttemptsTaken` is exposed in the response DTO.

No existing endpoints are modified.

---

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **4.3 — Output Validation and Retry** | `TryParseResponse` validates each attempt; on failure a new user turn listing specific errors is appended to the messages list before retrying — Claude sees its own previous output alongside the errors to fix |

---

## Architecture

```
POST /api/recipes/{id}/scale  { fromServings, toServings }
        │  ScaleRecipeHandler
        │  ├── validate servings > 0
        │  ├── IRecipeRepository.GetByIdAsync → Recipe
        │  ├── map to RecipeDto
        │  └── IRecipeScalingService.ScaleAsync(dto, from, to)
        │
        └── ClaudeRecipeScalingService
             │
             │  messages = [user: "Scale from N to M: {recipeJson}"]
             │
             │  loop attempt 1..3:
             │    SendAsync(messages) → rawJson
             │    TryParseResponse(rawJson) → (dto, errors)
             │    if errors empty → return dto
             │    append assistant: rawJson
             │    append user: "Validation errors:\n- ...\nPlease correct and resubmit."
             │
             └── throw if all 3 attempts fail
```

---

## ScaledRecipeDto

```csharp
record ScaledRecipeDto(
    Guid RecipeId,
    string Name,
    int FromServings,
    int ToServings,
    IReadOnlyList<ScaledIngredientDto> Ingredients,
    int AttemptsTaken);

record ScaledIngredientDto(string Name, decimal Quantity, string Unit);
```

---

## Validation rules in `TryParseResponse`

| Rule | Error message |
|---|---|
| Response is valid JSON | "Response is not valid JSON: {ex.Message}" |
| Root is a JSON object | "Response root must be a JSON object, not an array or scalar." |
| `ingredients` field is a non-empty array | "Field 'ingredients' is missing or not an array." / "must not be empty." |
| Each ingredient has non-empty `name` | "ingredients[N].name is missing or empty." |
| Each ingredient has `quantity > 0` | "ingredients[N].quantity must be a positive number (got X)." |

---

## Multi-turn retry message construction

```
Turn 1 — initial request:
  messages[0] = { role: "user", content: "Scale from 2 to 4: {recipeJson}" }

Validation fails → Turn 2:
  messages[1] = { role: "assistant", content: rawJson }
  messages[2] = { role: "user",      content: "Validation errors:\n- ...\nPlease correct and resubmit." }

Validation fails → Turn 3:
  messages[3] = { role: "assistant", content: rawJson2 }
  messages[4] = { role: "user",      content: "Validation errors:\n- ...\nPlease correct and resubmit." }
```

---

## Files to create

| Path | Purpose |
|---|---|
| `Backend/src/Recipes.Application/Recipes/ScaleRecipe/ScaledRecipeDto.cs` | Response DTOs |
| `Backend/src/Recipes.Application/Recipes/ScaleRecipe/IRecipeScalingService.cs` | Service interface |
| `Backend/src/Recipes.Application/Recipes/ScaleRecipe/ScaleRecipeCommand.cs` | Command + handler |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Services/ClaudeRecipeScalingService.cs` | Service implementation |
| `Backend/Docs/CCAF/P2-output-validation-retry.md` | CCAF documentation |

## Files to modify

| Path | Change |
|---|---|
| `Backend/src/Recipes.Infrastructure/DependencyInjection.cs` | Register `IRecipeScalingService` |
| `Backend/src/Recipes.Api/Endpoints/RecipesEndpoints.cs` | Add `POST /{id}/scale`, `ScaleRecipeRequest` record |

---

## Acceptance criteria

1. `dotnet build Backend/Recipes.sln` passes.
2. `POST /api/recipes/{id}/scale` with `fromServings: 0` returns 400.
3. `POST /api/recipes/{id}/scale` with unknown recipe ID returns 404.
4. A valid response has `attemptsTaken >= 1`.
5. `AttemptsTaken` reflects the actual number of Claude calls made.
6. On a simulated bad Claude response (all quantities = 0), the service retries up to 3 times then throws.
