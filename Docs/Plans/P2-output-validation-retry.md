# P2 — Output Validation and Retry Loop: Implementation Plan

Reference spec: `Docs/specs/P2-output-validation-retry.md`

Build order: DTOs → interface → command+handler → service → DI → endpoint → CCAF doc.

---

## Step 1 — DTOs

Create `Backend/src/Recipes.Application/Recipes/ScaleRecipe/ScaledRecipeDto.cs`:

```csharp
public sealed record ScaledRecipeDto(
    Guid RecipeId,
    string Name,
    int FromServings,
    int ToServings,
    IReadOnlyList<ScaledIngredientDto> Ingredients,
    int AttemptsTaken);

public sealed record ScaledIngredientDto(string Name, decimal Quantity, string Unit);
```

---

## Step 2 — Interface

Create `Backend/src/Recipes.Application/Recipes/ScaleRecipe/IRecipeScalingService.cs`:

```csharp
public interface IRecipeScalingService
{
    Task<ScaledRecipeDto> ScaleAsync(
        RecipeDto recipe, int fromServings, int toServings, CancellationToken ct);
}
```

---

## Step 3 — Command + handler

Create `Backend/src/Recipes.Application/Recipes/ScaleRecipe/ScaleRecipeCommand.cs`:

```csharp
public sealed record ScaleRecipeCommand(Guid RecipeId, int FromServings, int ToServings)
    : IRequest<ErrorOr<ScaledRecipeDto>>;
```

Handler:
1. Validate `FromServings > 0 && ToServings > 0` → `Error.Validation` if not
2. `GetByIdAsync` → 404 if null
3. Map to `RecipeDto`
4. Call `_scalingService.ScaleAsync(dto, from, to, ct)`

---

## Step 4 — Service implementation

Create `Backend/src/Recipes.Infrastructure/AI/Claude/Services/ClaudeRecipeScalingService.cs`:

**Constructor**: `IHttpClientFactory` (→ `"ClaudeAgent"`), `IOptions<ClaudeOptions>`, `ILogger`

**`ScaleAsync` loop (max 3 attempts)**:
```csharp
var messages = new List<ClaudeMessage>
{
    new("user", [new ClaudeContentBlock("text", $"Scale from {from} to {to} (factor={factor:F4}):\n\n{recipeJson}")])
};

for (int attempt = 1; attempt <= 3; attempt++)
{
    var rawJson = await SendAsync(messages, ct);
    var (dto, errors) = TryParseResponse(rawJson, ...);
    if (errors.Count == 0) return dto!;

    if (attempt < 3)
    {
        messages.Add(new("assistant", [new ClaudeContentBlock("text", rawJson)]));
        messages.Add(new("user", [new ClaudeContentBlock("text",
            $"Validation errors:\n{string.Join("\n", errors.Select(e => $"- {e}"))}\n\nPlease correct and resubmit.")]));
    }
}
throw new InvalidOperationException("...");
```

**`TryParseResponse`**: `JsonNode.Parse` → check root type → check `ingredients` array →
check each item for `name`, `quantity > 0`, `unit`. Return `(dto, errors)` tuple.

**`SendAsync`**: POST `ClaudeMessagesRequest` with the accumulated messages list.

---

## Step 5 — DI registration

```csharp
using Recipes.Application.Recipes.ScaleRecipe;
// ...
services.AddScoped<IRecipeScalingService, ClaudeRecipeScalingService>();
```

---

## Step 6 — Endpoint + request record

```csharp
using Recipes.Application.Recipes.ScaleRecipe;
// ...
group.MapPost("/{id:guid}/scale", async (Guid id, ScaleRecipeRequest request, ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(new ScaleRecipeCommand(id, request.FromServings, request.ToServings), ct);
    return result.ToHttpResult(dto => Results.Ok(dto));
});

// at bottom of file:
public sealed record ScaleRecipeRequest(int FromServings, int ToServings);
```

---

## Step 7 — CCAF doc

Create `Backend/Docs/CCAF/P2-output-validation-retry.md` covering:
- What this implements
- CCAF subtopics table
- Architecture diagram showing all three turns
- Key decisions (multi-turn vs. fresh retry, AttemptsTaken, constant system prompt)
