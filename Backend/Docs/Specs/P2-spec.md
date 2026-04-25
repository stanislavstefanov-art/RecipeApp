# P2 Spec — Output Validation and Retry Loop (Recipe Scaling)

## Goal

Add a recipe-scaling endpoint that sends a scaling request to Claude and validates the
response against an explicit schema. When validation fails, the service appends the
assistant's raw output and a user message listing each specific error to the conversation,
then retries. Up to three attempts are made. The retry path is multi-turn — Claude sees
its own previous output and the exact errors to fix.

## CCAF subtopics targeted

| Subtopic | How covered |
|---|---|
| **4.3 — Output Validation and Retry** | `TryParseResponse` validates each attempt against structural rules; on failure a new user turn is appended with the specific error list; Claude corrects in context |

## Proposed API

```
POST /api/recipes/{id}/scale
Body: { "fromServings": 2, "toServings": 4 }
→ 200 ScaledRecipeDto
→ 400 if servings invalid
→ 404 if recipe not found
→ 500 if all 3 attempts fail validation
```

### ScaledRecipeDto

```json
{
  "recipeId": "...",
  "name": "Classic Tomato Pasta",
  "fromServings": 2,
  "toServings": 4,
  "ingredients": [
    { "name": "spaghetti", "quantity": 400, "unit": "g" }
  ],
  "attemptsTaken": 1
}
```

## Retry loop design

```
messages = [user: "Scale this recipe from 2 to 4 servings: {...}"]

attempt 1:
  → call Claude
  → TryParseResponse → valid → return dto (attemptsTaken=1)
  → TryParseResponse → errors:
      messages += [assistant: rawJson]
      messages += [user: "Validation errors:\n- ...\nPlease correct and resubmit."]

attempt 2:
  → call Claude with extended messages
  → TryParseResponse → valid → return dto (attemptsTaken=2)
  → ...

attempt 3 (final):
  → TryParseResponse → fail → throw InvalidOperationException
```

## Validation rules in `TryParseResponse`

1. Response is valid JSON
2. Root is an object (not array or scalar)
3. `ingredients` field is a non-empty array
4. Each ingredient: non-empty `name`, `quantity > 0`, `unit` present

## Key design decisions

- **Multi-turn feedback, not fresh retry** — the distinguishing pattern vs. existing
  `RecipeImportOrchestrator`. Appending error detail in context lets Claude make targeted
  corrections rather than regenerating from scratch.
- **`AttemptsTaken` in the response DTO** — makes the retry behaviour observable without
  additional log scraping; a caller seeing `attemptsTaken: 2` knows one correction was needed.
- **System prompt is a constant** — scaling is a narrow, deterministic task; no few-shot
  examples needed. The service focuses on demonstrating the retry loop, not prompt composition.
- **`IRecipeScalingService` + `ClaudeRecipeScalingService`** — same interface/impl pattern
  as P1 and existing features.

## File plan

### Create
- `Backend/src/Recipes.Application/Recipes/ScaleRecipe/ScaledRecipeDto.cs`
- `Backend/src/Recipes.Application/Recipes/ScaleRecipe/IRecipeScalingService.cs`
- `Backend/src/Recipes.Application/Recipes/ScaleRecipe/ScaleRecipeCommand.cs` (command + handler)
- `Backend/src/Recipes.Infrastructure/AI/Claude/Services/ClaudeRecipeScalingService.cs`
- `Backend/Docs/CCAF/P2-output-validation-retry.md`

### Modify
- `Backend/src/Recipes.Infrastructure/DependencyInjection.cs` — register `IRecipeScalingService`
- `Backend/src/Recipes.Api/Endpoints/RecipesEndpoints.cs` — add `POST /{id}/scale`, add `ScaleRecipeRequest` record
