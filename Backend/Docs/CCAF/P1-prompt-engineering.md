# P1 — Structured Prompt Engineering

## What this implements

`POST /api/recipes/{id}/critique` runs a recipe through a Claude call whose system prompt
is assembled by `PromptBuilder` — a fluent utility that composes a prompt from six labeled
sections (ROLE, INPUT SPECIFICATION, OUTPUT SCHEMA, SUCCESS CRITERIA, FAILURE MODES,
EXAMPLES). The response is a structured `RecipeCritiqueDto` scoring the recipe on three
dimensions: clarity, completeness, and technique.

---

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **System Prompts with Explicit Criteria (4.1)** | `PromptBuilder.WithSuccessCriteria` and `WithFailureModes` emit labeled `## SUCCESS CRITERIA` and `## FAILURE MODES` sections — each criterion is a declarative, verifiable rule Claude must satisfy, not free prose instructions |
| **Few-Shot Prompting (4.4)** | `PromptBuilder.WithExamples` appends a `## EXAMPLES` section containing two fully worked input→output pairs: one high-quality recipe that scores 4–5 across all dimensions, one degenerate recipe that scores 1–2 with `needsReview: true` |

---

## Architecture

```
POST /api/recipes/{id}/critique
        │  CritiqueRecipeHandler
        │  ├── IRecipeRepository.GetByIdAsync → Recipe
        │  ├── map to RecipeDto
        │  └── IRecipeCritiqueService.CritiqueAsync(dto) → RecipeCritiqueDto
        │
        └── ClaudeRecipeCritiqueService
             ├── PromptBuilder.Build() → system prompt (once, at construction)
             └── POST /v1/messages → Claude → parse JSON → RecipeCritiqueDto
```

---

## Key decisions

### PromptBuilder composes rather than interpolates

Earlier features inject a prompt *template* from a file and embed the JSON schema by
string interpolation. `PromptBuilder` instead composes the prompt **structurally** — each
section is produced by a dedicated method (`WithRole`, `WithSuccessCriteria`, etc.) and
renders with a consistent `## HEADING` label. This makes individual sections testable in
isolation and ensures every prompt built with it is self-documenting.

### SUCCESS CRITERIA vs. FAILURE MODES sections

`WithSuccessCriteria` enumerates what a correct response looks like (invariants that must
always hold). `WithFailureModes` enumerates the edge-case inputs that require special
handling. Separating them mirrors the exam distinction between *output constraints* and
*input guards*, and gives Claude a checklist it can verify before emitting its response.

### Prompt built at construction time, not per-request

The system prompt does not embed the recipe content — that arrives in the user message.
Building the prompt once in the constructor avoids re-allocating the same 2 KB string on
every request and makes caching straightforward if extended later.

### Two contrasting few-shot examples

Example 1 is a high-quality five-ingredient pasta recipe with precise timing, correct
techniques, and complete units. It demonstrates the expected output shape *and* shows
what a high-confidence, `needsReview: false` result looks like.

Example 2 is a degenerate three-ingredient "Chicken Stew" with zero quantities and
one-word step instructions. It demonstrates that all three dimensions can bottom out at 1,
that `suggestions` are then mandatory, and that `needsReview: true` is triggered whenever
any rating falls below 3. The contrast trains Claude to discriminate rather than drift
toward average scores.
