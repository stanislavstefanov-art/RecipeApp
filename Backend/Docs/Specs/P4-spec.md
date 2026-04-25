# P4 Spec — LLM-as-Judge (Recipe Draft Jury)

## Goal

Add an endpoint that extracts a recipe draft from raw text, then submits that draft to
three independent Claude reviewer instances running concurrently. Each reviewer evaluates
the draft from a distinct lens (ingredient completeness, technique, clarity). A majority
vote (≥2 matching verdicts) determines the consensus. The full response includes the
draft, the consensus, and each juror's individual verdict with issues.

## CCAF subtopics targeted

| Subtopic | How covered |
|---|---|
| **4.6 — LLM-as-Judge / Multi-instance evaluation** | `Task.WhenAll` dispatches three independent `POST /v1/messages` calls concurrently, each with a different system-prompt persona; `AggregateVerdicts` applies majority voting |

## Proposed API

```
POST /api/recipes/import/with-jury
Body: { "text": "raw recipe text..." }
→ 200 RecipeDraftReviewDto
→ 400 if text empty or import fails
```

### RecipeDraftReviewDto

```json
{
  "draft": { ...ImportedRecipeDto... },
  "consensus": "approve" | "revise" | "reject",
  "jurorVerdicts": [
    { "jurorIndex": 1, "role": "Ingredient Specialist",
      "verdict": "approve", "confidence": "high", "issues": [] },
    { "jurorIndex": 2, "role": "Technique Specialist",
      "verdict": "revise", "confidence": "medium",
      "issues": ["No temperatures specified."] },
    { "jurorIndex": 3, "role": "Clarity Specialist",
      "verdict": "approve", "confidence": "high", "issues": [] }
  ]
}
```

## Juror personas

| Index | Role | System prompt focus |
|---|---|---|
| 1 | Ingredient Specialist | Quantity completeness, unit presence, ingredient specificity |
| 2 | Technique Specialist | Cooking method, timing, temperature, doneness cues |
| 3 | Clarity Specialist | Accessibility for home cooks, step order, ambiguous terms |

## Aggregation rule

```
≥2 "approve" → consensus = "approve"
≥2 "reject"  → consensus = "reject"
otherwise    → consensus = "revise"  (safe default for any split or tie)
```

## Key design decisions

- **Three different personas** — identical prompts would produce near-identical outputs.
  Distinct lenses create genuine disagreement, making the consensus semantically meaningful.
- **`Task.WhenAll` for true concurrency** — the three HTTP calls are data-independent;
  parallel dispatch cuts total latency to ≈1 single-call duration instead of ×3.
- **Non-fatal per-juror failure** — a single reviewer call failing returns a synthetic
  `verdict: "revise"` rather than throwing; consensus is computed from the two remaining
  valid votes.
- **Two-step handler** — `ReviewRecipeDraftHandler` calls `IRecipeImportOrchestrator`
  first (reusing existing extraction + validation), then passes the structured DTO to
  `IRecipeDraftReviewService`. Extraction and review are separate concerns.
- **Reuses `POST /import` request record** — the endpoint accepts `ImportRecipeRequest`
  (same `{ text }` body as existing import), so no new request record is needed.

## File plan

### Create
- `Backend/src/Recipes.Application/Recipes/ReviewRecipeDraft/RecipeDraftReviewDto.cs`
- `Backend/src/Recipes.Application/Recipes/ReviewRecipeDraft/IRecipeDraftReviewService.cs`
- `Backend/src/Recipes.Application/Recipes/ReviewRecipeDraft/ReviewRecipeDraftCommand.cs` (command + handler)
- `Backend/src/Recipes.Infrastructure/AI/Claude/Services/ClaudeRecipeDraftReviewService.cs`
- `Backend/Docs/CCAF/P4-llm-as-judge.md`

### Modify
- `Backend/src/Recipes.Infrastructure/DependencyInjection.cs` — register `IRecipeDraftReviewService`
- `Backend/src/Recipes.Api/Endpoints/RecipesEndpoints.cs` — add `POST /import/with-jury`
