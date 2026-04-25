# P4 — LLM-as-Judge: Implementation Plan

Reference spec: `Docs/specs/P4-llm-as-judge.md`

Build order: DTOs → interface → command+handler → service → DI → endpoint → CCAF doc.

---

## Step 1 — DTOs

Create `Backend/src/Recipes.Application/Recipes/ReviewRecipeDraft/RecipeDraftReviewDto.cs`:

```csharp
public sealed record RecipeDraftReviewDto(
    ImportedRecipeDto Draft,
    string Consensus,
    IReadOnlyList<JurorVerdictDto> JurorVerdicts);

public sealed record JurorVerdictDto(
    int JurorIndex,
    string Role,
    string Verdict,
    string Confidence,
    IReadOnlyList<string> Issues);
```

Import: `using Recipes.Application.Recipes.ImportRecipeFromText;`

---

## Step 2 — Interface

Create `Backend/src/Recipes.Application/Recipes/ReviewRecipeDraft/IRecipeDraftReviewService.cs`:

```csharp
public interface IRecipeDraftReviewService
{
    Task<RecipeDraftReviewDto> ReviewAsync(ImportedRecipeDto draft, CancellationToken ct);
}
```

---

## Step 3 — Command + handler

Create `Backend/src/Recipes.Application/Recipes/ReviewRecipeDraft/ReviewRecipeDraftCommand.cs`:

```csharp
public sealed record ReviewRecipeDraftCommand(string RecipeText)
    : IRequest<ErrorOr<RecipeDraftReviewDto>>;
```

Handler:
1. Validate `RecipeText` not empty → `Error.Validation` if blank
2. `_importer.ImportAsync(request.RecipeText, ct)` → propagate error if `IsError`
3. `_reviewer.ReviewAsync(importResult.Value, ct)`
4. Return result

Dependencies injected: `IRecipeImportOrchestrator`, `IRecipeDraftReviewService`.

---

## Step 4 — Service implementation

Create `Backend/src/Recipes.Infrastructure/AI/Claude/Services/ClaudeRecipeDraftReviewService.cs`:

**Static reviewer definitions** (array of Role + SystemPrompt tuples):
```csharp
private static readonly (string Role, string SystemPrompt)[] Reviewers =
[
    ("Ingredient Specialist", "...ingredient-focused prompt + output schema..."),
    ("Technique Specialist",  "...technique-focused prompt + output schema..."),
    ("Clarity Specialist",    "...clarity-focused prompt + output schema...")
];
```

Each system prompt ends with the shared output schema:
```
Return ONLY valid JSON:
{ "verdict": "approve"|"revise"|"reject", "confidence": "high"|"medium"|"low", "issues": [...] }
```

**`ReviewAsync`**:
```csharp
var draftJson     = JsonSerializer.Serialize(draft, JsonOptions);
var reviewTasks   = Reviewers
    .Select((r, i) => CallReviewerAsync(r.Role, r.SystemPrompt, draftJson, i, ct))
    .ToList();
var jurorVerdicts = await Task.WhenAll(reviewTasks);
var consensus     = AggregateVerdicts(jurorVerdicts);
return new RecipeDraftReviewDto(draft, consensus, jurorVerdicts);
```

**`CallReviewerAsync`**:
- POST `ClaudeMessagesRequest` (system = reviewer prompt, user = draftJson)
- On non-2xx: return synthetic `JurorVerdictDto { verdict="revise", confidence="low", issues=["Call failed..."] }`
- On empty text: same fallback
- Otherwise: `ParseVerdict(text, index, role)`

**`ParseVerdict`**:
- `JsonNode.Parse(json)`
- Extract `verdict`, `confidence`, `issues` array
- Normalise verdict: only `"approve"` / `"reject"` pass through; anything else → `"revise"`
- On `JsonException`: return fallback

**`AggregateVerdicts`**:
```csharp
var counts = verdicts.GroupBy(v => v.Verdict).ToDictionary(g => g.Key, g => g.Count());
if (counts.GetValueOrDefault("approve") >= 2) return "approve";
if (counts.GetValueOrDefault("reject")  >= 2) return "reject";
return "revise";
```

---

## Step 5 — DI registration

```csharp
using Recipes.Application.Recipes.ReviewRecipeDraft;
// ...
services.AddScoped<IRecipeDraftReviewService, ClaudeRecipeDraftReviewService>();
```

---

## Step 6 — Endpoint

```csharp
using Recipes.Application.Recipes.ReviewRecipeDraft;
// ...
group.MapPost("/import/with-jury", async (ImportRecipeRequest request, ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(new ReviewRecipeDraftCommand(request.Text), ct);
    return result.ToHttpResult(dto => Results.Ok(dto));
});
```

Reuses the existing `ImportRecipeRequest` record — no new request type needed.

---

## Step 7 — CCAF doc

Create `Backend/Docs/CCAF/P4-llm-as-judge.md` covering:
- What this implements
- CCAF subtopics table
- Architecture diagram
- Key decisions (three personas vs. identical prompts, Task.WhenAll concurrency,
  non-fatal per-juror failure, majority vote rule, two-step handler)
