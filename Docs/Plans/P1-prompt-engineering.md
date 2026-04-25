# P1 — Structured Prompt Builder + Recipe Critique: Implementation Plan

Reference spec: `Docs/specs/P1-prompt-engineering.md`

Build order: PromptBuilder → DTOs → interface → command+handler →
service → DI → endpoint → CCAF doc.

---

## Step 1 — PromptBuilder

Create `Backend/src/Recipes.Application/Common/AI/PromptBuilder.cs`:

```csharp
public sealed record PromptExample(string Description, string Input, string Output);

public sealed class PromptBuilder
{
    private string? _role, _inputSpec, _outputSchema;
    private IReadOnlyList<string> _successCriteria = [];
    private IReadOnlyList<string> _failureModes    = [];
    private IReadOnlyList<PromptExample> _examples = [];

    public PromptBuilder WithRole(string role)                       { _role            = role;     return this; }
    public PromptBuilder WithInputSpec(string spec)                  { _inputSpec       = spec;     return this; }
    public PromptBuilder WithOutputSchema(string schema)             { _outputSchema    = schema;   return this; }
    public PromptBuilder WithSuccessCriteria(params string[] c)      { _successCriteria = c;        return this; }
    public PromptBuilder WithFailureModes(params string[] m)         { _failureModes    = m;        return this; }
    public PromptBuilder WithExamples(params PromptExample[] e)      { _examples        = e;        return this; }

    public string Build()
    {
        // append ## HEADING sections for non-empty fields
        // append ## EXAMPLES with numbered ### Example N blocks
    }
}
```

---

## Step 2 — DTOs

Create `Backend/src/Recipes.Application/Recipes/CritiqueRecipe/RecipeCritiqueDto.cs`:

```csharp
public sealed record RecipeCritiqueDto(
    IReadOnlyList<CritiqueDimensionDto> Dimensions,
    string Summary,
    string Confidence,
    bool NeedsReview);

public sealed record CritiqueDimensionDto(
    string Dimension,
    int Rating,
    string Rationale,
    IReadOnlyList<string> Suggestions);
```

---

## Step 3 — Interface

Create `Backend/src/Recipes.Application/Recipes/CritiqueRecipe/IRecipeCritiqueService.cs`:

```csharp
public interface IRecipeCritiqueService
{
    Task<RecipeCritiqueDto> CritiqueAsync(RecipeDto recipe, CancellationToken ct);
}
```

Takes `RecipeDto` (from `GetRecipeQuery`) — the handler fetches the recipe and maps it.

---

## Step 4 — Command + handler

Create `Backend/src/Recipes.Application/Recipes/CritiqueRecipe/CritiqueRecipeCommand.cs`:

```csharp
public sealed record CritiqueRecipeCommand(Guid RecipeId)
    : IRequest<ErrorOr<RecipeCritiqueDto>>;
```

Handler:
1. `RecipeId.From(request.RecipeId)` → `GetByIdAsync` → 404 if null
2. Map recipe to `RecipeDto`
3. Call `_critiqueService.CritiqueAsync(dto, ct)`
4. Return result

---

## Step 5 — Service implementation

Create `Backend/src/Recipes.Infrastructure/AI/Claude/Services/ClaudeRecipeCritiqueService.cs`:

- Constructor: `IHttpClientFactory` (creates `"ClaudeAgent"` client), `IOptions<ClaudeOptions>`, `ILogger`
- Build system prompt once in constructor via `PromptBuilder`:
  - `.WithRole(...)` — expert culinary critic
  - `.WithInputSpec(...)` — recipe JSON fields
  - `.WithOutputSchema(...)` — schema + "Return ONLY valid JSON, no markdown fences"
  - `.WithSuccessCriteria(...)` — 5 rules (rating range, array order, specific rationale, etc.)
  - `.WithFailureModes(...)` — 4 edge-case guards
  - `.WithExamples(example1, example2)` — pasta (high scores) and chicken stew (low scores)
- `CritiqueAsync`: serialize `RecipeDto` → build `ClaudeMessagesRequest` → POST → deserialize `RecipeCritiqueDto`

---

## Step 6 — DI registration

In `Backend/src/Recipes.Infrastructure/DependencyInjection.cs`:

```csharp
using Recipes.Application.Recipes.CritiqueRecipe;
// ...
services.AddScoped<IRecipeCritiqueService, ClaudeRecipeCritiqueService>();
```

---

## Step 7 — Endpoint

In `Backend/src/Recipes.Api/Endpoints/RecipesEndpoints.cs`:

```csharp
using Recipes.Application.Recipes.CritiqueRecipe;
// ...
group.MapPost("/{id:guid}/critique", async (Guid id, ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(new CritiqueRecipeCommand(id), ct);
    return result.ToHttpResult(dto => Results.Ok(dto));
});
```

---

## Step 8 — CCAF doc

Create `Backend/Docs/CCAF/P1-prompt-engineering.md` covering:
- What this implements
- CCAF subtopics table
- Architecture diagram
- Key decisions (PromptBuilder sections, prompt-at-construction, two contrasting examples)
