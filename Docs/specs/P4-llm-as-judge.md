# P4 — LLM-as-Judge (Recipe Draft Jury)

## Summary

Adds `POST /api/recipes/import/with-jury` that extracts a recipe draft from raw text
using the existing `IRecipeImportOrchestrator`, then submits that draft to three
independent Claude reviewer instances running concurrently via `Task.WhenAll`. Each
reviewer uses a distinct system-prompt persona. A majority vote (≥2 matching verdicts)
produces the consensus (`approve` / `revise` / `reject`). The response includes the
draft, the consensus, and each juror's individual verdict with issues.

No existing endpoints are modified.

---

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **4.6 — LLM-as-Judge / Multi-instance evaluation** | `ClaudeRecipeDraftReviewService.ReviewAsync` dispatches three independent Claude calls concurrently via `Task.WhenAll`, each with a different system-prompt persona; `AggregateVerdicts` applies majority voting |

---

## Architecture

```
POST /api/recipes/import/with-jury  { text: "raw recipe text" }
        │  ReviewRecipeDraftHandler
        │  ├── IRecipeImportOrchestrator.ImportAsync(text) → ImportedRecipeDto
        │  └── IRecipeDraftReviewService.ReviewAsync(draft)
        │
        └── ClaudeRecipeDraftReviewService.ReviewAsync
             │
             ├── Task.WhenAll([
             │     CallReviewerAsync(1, "Ingredient Specialist", systemPrompt1, draftJson),
             │     CallReviewerAsync(2, "Technique Specialist",  systemPrompt2, draftJson),
             │     CallReviewerAsync(3, "Clarity Specialist",    systemPrompt3, draftJson)
             │   ])
             │   ↳ three concurrent POST /v1/messages, fully independent
             │
             └── AggregateVerdicts([j1, j2, j3])
                  ≥2 "approve" → "approve"
                  ≥2 "reject"  → "reject"
                  otherwise    → "revise"
```

---

## DTOs

```csharp
record RecipeDraftReviewDto(
    ImportedRecipeDto Draft,
    string Consensus,                          // "approve" | "revise" | "reject"
    IReadOnlyList<JurorVerdictDto> JurorVerdicts);

record JurorVerdictDto(
    int JurorIndex,
    string Role,
    string Verdict,       // "approve" | "revise" | "reject"
    string Confidence,    // "high" | "medium" | "low"
    IReadOnlyList<string> Issues);
```

---

## Juror personas

| Index | Role | Evaluation focus |
|---|---|---|
| 1 | Ingredient Specialist | Quantity completeness, unit presence, ingredient specificity |
| 2 | Technique Specialist | Cooking method, timing, temperature, doneness cues |
| 3 | Clarity Specialist | Home-cook accessibility, step order, ambiguous terms |

Each system prompt has a shared output schema:
```json
{ "verdict": "approve"|"revise"|"reject", "confidence": "high"|"medium"|"low", "issues": [...] }
```

---

## Aggregation rule

```
≥2 "approve" → consensus = "approve"
≥2 "reject"  → consensus = "reject"
otherwise    → consensus = "revise"   (safe default for any split or three-way tie)
```

---

## Per-juror failure handling

If a reviewer HTTP call fails (network error, non-2xx), `CallReviewerAsync` returns a
synthetic `JurorVerdictDto` with `verdict: "revise"`, `confidence: "low"` and an
error message in `issues` — rather than throwing. Consensus is computed from remaining
valid votes.

---

## Files to create

| Path | Purpose |
|---|---|
| `Backend/src/Recipes.Application/Recipes/ReviewRecipeDraft/RecipeDraftReviewDto.cs` | Response DTOs |
| `Backend/src/Recipes.Application/Recipes/ReviewRecipeDraft/IRecipeDraftReviewService.cs` | Service interface |
| `Backend/src/Recipes.Application/Recipes/ReviewRecipeDraft/ReviewRecipeDraftCommand.cs` | Command + handler |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Services/ClaudeRecipeDraftReviewService.cs` | Service implementation |
| `Backend/Docs/CCAF/P4-llm-as-judge.md` | CCAF documentation |

## Files to modify

| Path | Change |
|---|---|
| `Backend/src/Recipes.Infrastructure/DependencyInjection.cs` | Register `IRecipeDraftReviewService` |
| `Backend/src/Recipes.Api/Endpoints/RecipesEndpoints.cs` | Add `POST /import/with-jury` (reuses `ImportRecipeRequest`) |

---

## Acceptance criteria

1. `dotnet build Backend/Recipes.sln` passes.
2. `POST /api/recipes/import/with-jury` with empty text returns 400.
3. Response `jurorVerdicts` always has exactly 3 entries.
4. `consensus` matches the majority of the three `verdict` values.
5. A failed individual reviewer call does not cause the endpoint to return 5xx — it
   returns a degraded result with that juror's `issues` noting the failure.
6. Existing `POST /api/recipes/import` endpoint unchanged.
