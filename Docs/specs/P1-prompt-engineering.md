# P1 — Structured Prompt Builder + Recipe Critique

## Summary

Adds a `PromptBuilder` utility that composes Claude system prompts from six labeled
sections (ROLE, INPUT SPECIFICATION, OUTPUT SCHEMA, SUCCESS CRITERIA, FAILURE MODES,
EXAMPLES). Uses it to build the prompt for a new `POST /api/recipes/{id}/critique`
endpoint that evaluates a recipe on three quality dimensions and returns a structured
`RecipeCritiqueDto`.

No existing endpoints are modified.

---

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **4.1 — System Prompts with Explicit Criteria** | `PromptBuilder.WithSuccessCriteria` and `WithFailureModes` emit labeled `## SUCCESS CRITERIA` / `## FAILURE MODES` sections — each item is a declarative, verifiable rule |
| **4.4 — Few-Shot Prompting** | `PromptBuilder.WithExamples` appends a `## EXAMPLES` section; the critique prompt includes two contrasting fully-worked input→output examples |

---

## Architecture

```
POST /api/recipes/{id}/critique
        │  CritiqueRecipeHandler
        │  ├── IRecipeRepository.GetByIdAsync → Recipe
        │  ├── map to RecipeDto
        │  └── IRecipeCritiqueService.CritiqueAsync(dto)
        │
        └── ClaudeRecipeCritiqueService
             ├── PromptBuilder.Build() → system prompt (once, at construction)
             └── POST /v1/messages → Claude → parse JSON → RecipeCritiqueDto
```

---

## RecipeCritiqueDto

```csharp
record RecipeCritiqueDto(
    IReadOnlyList<CritiqueDimensionDto> Dimensions,
    string Summary,
    string Confidence,
    bool NeedsReview);

record CritiqueDimensionDto(
    string Dimension,   // "clarity" | "completeness" | "technique"
    int Rating,         // 1–5
    string Rationale,
    IReadOnlyList<string> Suggestions);
```

---

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

record PromptExample(string Description, string Input, string Output);
```

Each section renders as `## HEADING` + content. `WithExamples` renders numbered
`### Example N: Description` blocks with `**Input:**` / `**Output:**` pairs.

---

## Few-shot examples in the critique prompt

| Example | Recipe | Expected output |
|---|---|---|
| 1 | Classic Tomato Pasta — complete ingredients, precise timing, correct technique | clarity=5, completeness=4, technique=5, needsReview=false |
| 2 | Chicken Stew — zero quantities, one-word step instructions | clarity=1, completeness=1, technique=2, needsReview=true |

---

## Files to create

| Path | Purpose |
|---|---|
| `Backend/src/Recipes.Application/Common/AI/PromptBuilder.cs` | `PromptBuilder` + `PromptExample` |
| `Backend/src/Recipes.Application/Recipes/CritiqueRecipe/RecipeCritiqueDto.cs` | Response DTO |
| `Backend/src/Recipes.Application/Recipes/CritiqueRecipe/IRecipeCritiqueService.cs` | Service interface |
| `Backend/src/Recipes.Application/Recipes/CritiqueRecipe/CritiqueRecipeCommand.cs` | Command + handler |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Services/ClaudeRecipeCritiqueService.cs` | Service implementation |
| `Backend/Docs/CCAF/P1-prompt-engineering.md` | CCAF documentation |

## Files to modify

| Path | Change |
|---|---|
| `Backend/src/Recipes.Infrastructure/DependencyInjection.cs` | Register `IRecipeCritiqueService` |
| `Backend/src/Recipes.Api/Endpoints/RecipesEndpoints.cs` | Add `POST /{id}/critique` |

---

## Acceptance criteria

1. `dotnet build Backend/Recipes.sln` passes.
2. `POST /api/recipes/{id}/critique` returns 404 for an unknown recipe ID.
3. Response `dimensions` array contains exactly three entries in order: clarity, completeness, technique.
4. `needsReview` is `true` when any dimension rating is below 3.
5. The built system prompt contains `## SUCCESS CRITERIA`, `## FAILURE MODES`, and `## EXAMPLES` sections.
6. Existing `/api/recipes/import` and all other recipe endpoints unchanged.
