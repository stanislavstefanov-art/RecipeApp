# P4 — LLM-as-Judge

## What this implements

`POST /api/recipes/import/with-jury` accepts raw recipe text, extracts a draft using the
existing `IRecipeImportOrchestrator`, then submits that draft to three independent Claude
reviewer instances running concurrently via `Task.WhenAll`. Each reviewer has a distinct
system prompt (Ingredient Specialist, Technique Specialist, Clarity Specialist) and
returns `{ verdict, confidence, issues }`. A majority vote over the three verdicts
produces the consensus (`approve` / `revise` / `reject`). The full response includes
the draft, the consensus, and each juror's individual verdict.

---

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **LLM-as-Judge / Multi-instance evaluation (4.6)** | `ClaudeRecipeDraftReviewService.ReviewAsync` dispatches three independent Claude calls concurrently via `Task.WhenAll`. Each call uses a different system prompt persona. `AggregateVerdicts` applies majority voting: ≥2 identical verdicts determines the consensus; a three-way split returns `"revise"` as the safe default |

---

## Architecture

```
POST /api/recipes/import/with-jury
        │  ReviewRecipeDraftHandler
        │  ├── IRecipeImportOrchestrator.ImportAsync(text) → ImportedRecipeDto
        │  └── IRecipeDraftReviewService.ReviewAsync(draft) → RecipeDraftReviewDto
        │
        └── ClaudeRecipeDraftReviewService.ReviewAsync
             │
             ├── Task.WhenAll([
             │     CallReviewerAsync("Ingredient Specialist", systemPrompt1, draftJson),
             │     CallReviewerAsync("Technique Specialist",  systemPrompt2, draftJson),
             │     CallReviewerAsync("Clarity Specialist",    systemPrompt3, draftJson)
             │   ])
             │   ↳ three concurrent POST /v1/messages calls, each independent
             │
             └── AggregateVerdicts([juror1, juror2, juror3])
                  ├── 2+ "approve"  → consensus = "approve"
                  ├── 2+ "reject"   → consensus = "reject"
                  └── otherwise     → consensus = "revise"
```

---

## Key decisions

### Three personas, not three identical calls

Running three calls with identical prompts would likely produce near-identical outputs
(particularly at low temperatures). Each reviewer has a distinct evaluation lens:
ingredient completeness, cooking technique, and home-cook clarity. The persona difference
ensures genuine disagreement is possible and makes the consensus semantically meaningful —
a recipe might be ingredient-complete (approve) but technically flawed (reject) and
confusingly written (reject), yielding a consensus of "reject" from a two-thirds majority.

### `Task.WhenAll` for true concurrency

The three HTTP calls to Claude are independent and have no data dependency on each other.
`Task.WhenAll` dispatches them simultaneously rather than sequentially. Total latency is
approximately that of the slowest single call (~1–2 s) rather than three sequential calls
(~3–6 s). This is also an efficiency gain: all three requests share the HTTP connection
pool and may run in different Anthropic worker threads.

### Non-fatal per-juror failures

If one reviewer call fails (network error, Claude timeout, bad status), `CallReviewerAsync`
returns a synthetic `JurorVerdictDto` with `verdict = "revise"` rather than throwing.
This ensures the consensus is still computed with the two remaining valid votes — the
feature degrades gracefully rather than failing entirely.

### Majority vote, not unanimous consensus

A unanimous rule would make the jury too conservative (one lenient reviewer blocks
approval). A simple majority (2 of 3) balances coverage: two independent reviewers
agreeing on approval is a stronger signal than one dissenting voice, and two agreeing
on rejection is sufficient reason to flag a draft for revision.

### Reuses existing import pipeline

`ReviewRecipeDraftHandler` calls `IRecipeImportOrchestrator.ImportAsync` first, reusing
the existing Claude extraction + validation flow unchanged. The review service only sees
the structured `ImportedRecipeDto`, not the raw text — clean separation of concerns
between extraction accuracy and review quality.
