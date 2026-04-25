# P1 Spec — Structured Prompt Builder + Recipe Critique

## Goal

Add a reusable `PromptBuilder` utility that composes Claude system prompts from labeled,
structured sections. Use it to build the prompt for a new recipe-critique endpoint that
evaluates a recipe on three quality dimensions and returns a structured critique DTO.

## CCAF subtopics targeted

| Subtopic | How covered |
|---|---|
| **4.1 — System Prompts with Explicit Criteria** | `PromptBuilder.WithSuccessCriteria` and `WithFailureModes` emit labeled `## SUCCESS CRITERIA` / `## FAILURE MODES` sections — each item is a declarative, verifiable rule |
| **4.4 — Few-Shot Prompting** | `PromptBuilder.WithExamples` appends a `## EXAMPLES` section; the critique prompt ships with two contrasting fully-worked examples |

## Proposed API

```
POST /api/recipes/{id}/critique
→ 200 RecipeCritiqueDto
→ 404 if recipe not found
```

### RecipeCritiqueDto

```json
{
  "dimensions": [
    { "dimension": "clarity", "rating": 4, "rationale": "...", "suggestions": ["..."] },
    { "dimension": "completeness", "rating": 3, "rationale": "...", "suggestions": ["..."] },
    { "dimension": "technique", "rating": 5, "rationale": "...", "suggestions": [] }
  ],
  "summary": "...",
  "confidence": "high" | "medium" | "low",
  "needsReview": false
}
```

## PromptBuilder API

```csharp
new PromptBuilder()
    .WithRole(string)
    .WithInputSpec(string)
    .WithOutputSchema(string)
    .WithSuccessCriteria(params string[])
    .WithFailureModes(params string[])
    .WithExamples(params PromptExample[])
    .Build() → string
```

`PromptExample(string Description, string Input, string Output)` — used by `WithExamples`.

## Key design decisions

- **`PromptBuilder` lives in `Recipes.Application/Common/AI/`** — pure string composition,
  no infrastructure dependency, reusable across features.
- **System prompt built once at construction** in `ClaudeRecipeCritiqueService` — the prompt
  does not embed the recipe; that arrives in the user message, so building it once avoids
  per-request allocation and positions it for caching later.
- **Two contrasting examples** — one high-quality recipe (scores 4–5) and one degenerate
  recipe (scores 1–2, `needsReview: true`) — to train Claude to discriminate rather than
  drift toward average scores.
- **`IRecipeCritiqueService` interface** in Application; `ClaudeRecipeCritiqueService` in
  Infrastructure — consistent with existing patterns (`IRecipeImportService` etc.).
- **Uses `IHttpClientFactory` with "ClaudeAgent" named client** — no new `AddHttpClient`
  typed registration needed since there is no separate client interface.

## File plan

### Create
- `Backend/src/Recipes.Application/Common/AI/PromptBuilder.cs`
- `Backend/src/Recipes.Application/Recipes/CritiqueRecipe/RecipeCritiqueDto.cs`
- `Backend/src/Recipes.Application/Recipes/CritiqueRecipe/IRecipeCritiqueService.cs`
- `Backend/src/Recipes.Application/Recipes/CritiqueRecipe/CritiqueRecipeCommand.cs` (command + handler)
- `Backend/src/Recipes.Infrastructure/AI/Claude/Services/ClaudeRecipeCritiqueService.cs`
- `Backend/Docs/CCAF/P1-prompt-engineering.md`

### Modify
- `Backend/src/Recipes.Infrastructure/DependencyInjection.cs` — register `IRecipeCritiqueService`
- `Backend/src/Recipes.Api/Endpoints/RecipesEndpoints.cs` — add `POST /{id}/critique`
