# F2 — Agentic Recipe Import from URL

## Summary

Add a new endpoint `POST /api/recipes/import/url` that accepts a URL and returns an
`ImportedRecipeDto` using a multi-turn Claude agentic loop. The loop gives Claude four
tools — URL fetching, field extraction, ingredient normalization (parallel), and a
terminal save tool — instead of a single prompt with a JSON schema.

The existing `POST /api/recipes/import` endpoint and all its backing code are
**not modified**. Both endpoints coexist and return the same `ImportedRecipeDto` shape.

---

## CCAF subtopics covered

| Subtopic | Where |
|---|---|
| Tool schema / `input_schema` authoring | Four tool definitions in `RecipeImportTools.cs` |
| Tool description writing (for model guidance) | Each tool's `description` field |
| Forced `tool_choice` | Not used here — `tool_choice: auto` exercises the model's own routing; the spec section below explains why |
| Multi-turn agentic loop (`stop_reason: tool_use`) | `RecipeImportAgent` loop |
| Parallel tool calls | Backend executes all `tool_use` blocks from one response concurrently via `Task.WhenAll` |
| Tool error handling (`is_error: true`) | `fetch_url_content` returns `is_error: true` on fetch failure; agent must decide whether to retry or abort |
| Structured output via terminal tool | `save_recipe` carries the final recipe; its input IS the output (no JSON parsing of text) |

> **Note on `tool_choice`:** The existing import uses JSON-in-prompt with no `tool_choice`.
> This feature uses `tool_choice: auto` so Claude decides the calling order — a more
> realistic agentic pattern. Forced tool choice (`{type: "tool", name: "..."}`) appears
> in a later feature (F4 prompts). Both patterns are covered across the feature set.

---

## API contract

### New endpoint

```
POST /api/recipes/import/url
Content-Type: application/json
Authorization: (same as other endpoints)
```

**Request body**
```json
{
  "sourceUrl": "https://www.seriouseats.com/spaghetti-carbonara-recipe"
}
```

| Field | Type | Rules |
|---|---|---|
| `sourceUrl` | `string` | Required. Must be absolute `http` or `https` URL. Max 2 048 chars. |

**Success response — `200 OK`**

Same shape as the existing import endpoint (`ImportedRecipeDto`):
```json
{
  "title": "Spaghetti Carbonara",
  "servings": 4,
  "ingredients": [
    { "name": "spaghetti", "quantity": 400, "unit": "g", "notes": null },
    { "name": "guanciale", "quantity": 150, "unit": "g", "notes": "or pancetta" }
  ],
  "steps": [
    "Bring a large pot of salted water to boil.",
    "..."
  ],
  "notes": null,
  "confidence": 0.92,
  "needsReview": false
}
```

**Error responses**

| Status | Condition |
|---|---|
| `400 Bad Request` | Validation failure (missing URL, not http/https, too long) |
| `422 Unprocessable Entity` | URL returned non-2xx, content too short, or loop ended without `save_recipe` |
| `504 Gateway Timeout` | Loop exceeded 10 iterations or 90-second wall time |
| `502 Bad Gateway` | Claude API returned non-2xx |

---

## Agentic loop design

### System prompt (brief)

```
You are a recipe extraction agent. You have tools to fetch a URL, extract recipe
fields from the text, normalize each ingredient, and submit the final recipe.

Work step by step:
1. Fetch the URL.
2. Extract title, servings, ingredients (raw), and steps.
3. Normalize every ingredient in parallel.
4. Call save_recipe with the final result.

If fetching fails, try once more. If it fails again, do not guess — call save_recipe
with needsReview: true and your best effort from any partial content.
```

### Conversation skeleton

```
Turn 0 — user
  "Extract the recipe from: https://..."

Turn 1 — assistant  (stop_reason: tool_use)
  tool_use: fetch_url_content({ url: "https://..." })

Turn 2 — user (tool_result)
  { content: "...cleaned page text..." }

Turn 3 — assistant  (stop_reason: tool_use)
  tool_use: extract_recipe_fields({ title: "...", ingredients: [...], steps: [...], ... })
  tool_use: normalize_ingredient({ name: "spaghetti", rawQuantity: "400", rawUnit: "g" })
  tool_use: normalize_ingredient({ name: "guanciale",  rawQuantity: "150", rawUnit: "g" })
  ... (one per ingredient, all in this same response — parallel)

Turn 4 — user (tool_results, all parallel results)
  { tool_use_id: "...", content: "{ ingredientCount: 6, ... }" }
  { tool_use_id: "...", content: "{ normalizedName: "spaghetti", quantity: 400, unit: "g" }" }
  { tool_use_id: "...", content: "{ normalizedName: "guanciale",  quantity: 150, unit: "g" }" }
  ...

Turn 5 — assistant  (stop_reason: tool_use)
  tool_use: save_recipe({ title: "...", ingredients: [...normalized...], ... })

→ Loop terminates. Orchestrator returns save_recipe input as ImportedRecipeDto.
```

### Loop termination rules

| Condition | Action |
|---|---|
| Claude calls `save_recipe` | Capture input as result, return `200 OK` |
| `stop_reason = "end_turn"` with no `save_recipe` | Return `422` — loop completed without saving |
| `stop_reason = "max_tokens"` | Return `422` |
| Iteration count ≥ 10 | Abort, return `504` |
| Wall time ≥ 90 s | Abort (CancellationToken), return `504` |

---

## Tool definitions

### `fetch_url_content`

**Purpose:** Fetch a webpage and return its text content.

**Input schema**
```json
{
  "type": "object",
  "required": ["url"],
  "additionalProperties": false,
  "properties": {
    "url": {
      "type": "string",
      "description": "Absolute http or https URL to fetch."
    }
  }
}
```

**Backend implementation**
- Named `HttpClient` `"RecipeUrlFetcher"` with 15 s timeout, `User-Agent: RecipeApp/1.0`.
- GET the URL, follow up to 5 redirects.
- Strip all HTML tags (regex or `HtmlAgilityPack`); keep visible text.
- Truncate to 50 000 chars.
- Block private / loopback ranges (127.x, 10.x, 172.16–31.x, 192.168.x) — SSRF guard.
- Return: `{ "content": "<cleaned text>" }` on success.
- Return: `is_error: true`, `{ "error": "<message>" }` on non-2xx, timeout, DNS failure, or blocked IP.

---

### `extract_recipe_fields`

**Purpose:** Claude submits its initial field extraction. Backend stores draft state and
tells Claude how many ingredients need normalization.

**Input schema**
```json
{
  "type": "object",
  "required": ["title", "servings", "ingredients", "steps"],
  "additionalProperties": false,
  "properties": {
    "title":       { "type": ["string", "null"] },
    "servings":    { "type": ["integer", "null"], "minimum": 1 },
    "ingredients": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["name"],
        "additionalProperties": false,
        "properties": {
          "name":        { "type": "string" },
          "rawQuantity": { "type": ["string", "null"], "description": "Free-form quantity text, e.g. '2½' or '3/4'." },
          "rawUnit":     { "type": ["string", "null"], "description": "Free-form unit text, e.g. 'tablespoons' or 'oz'." },
          "notes":       { "type": ["string", "null"] }
        }
      }
    },
    "steps": { "type": "array", "items": { "type": "string" } },
    "notes": { "type": ["string", "null"] }
  }
}
```

**Backend implementation**
- Validate: ingredients array must not be empty; steps must not be empty.
- Store draft in orchestrator-scoped state (in-memory, lives for the loop).
- Return: `{ "ingredientCount": N, "message": "Draft received. Call normalize_ingredient for each ingredient." }`
- On invalid input: return `is_error: true` with field-level error text.

---

### `normalize_ingredient`

**Purpose:** Canonicalize one ingredient's name, quantity, and unit.
Claude calls this once per ingredient, **all in the same response**, triggering parallel execution.

**Input schema**
```json
{
  "type": "object",
  "required": ["name"],
  "additionalProperties": false,
  "properties": {
    "name":        { "type": "string",          "description": "Ingredient name as extracted." },
    "rawQuantity": { "type": ["string", "null"], "description": "Free-form quantity text." },
    "rawUnit":     { "type": ["string", "null"], "description": "Free-form unit text." }
  }
}
```

**Backend implementation**
- Normalize `name`: lowercase, trim, collapse whitespace.
- Parse `rawQuantity` to `decimal?`: handle fractions (`3/4`), mixed numbers (`1½`, `1 1/2`), unicode fractions. Return `null` if unparseable.
- Normalize `rawUnit` to canonical short form using a static lookup table:
  `"tablespoon" | "tablespoons" | "tbsp" → "tbsp"`, `"teaspoon" | "tsp" → "tsp"`,
  `"cup" | "cups" → "cup"`, `"gram" | "grams" | "g" → "g"`, etc.
- Return: `{ "normalizedName": "...", "quantity": 2.5, "unit": "tbsp" }`
- Never returns `is_error: true` — normalization is best-effort, unknown units pass through unchanged.

---

### `save_recipe`

**Purpose:** Terminal tool. Claude calls this once all ingredients are normalized to
submit the final recipe. The orchestrator treats this call as loop completion.

**Input schema**
```json
{
  "type": "object",
  "required": ["title", "ingredients", "steps", "confidence", "needsReview"],
  "additionalProperties": false,
  "properties": {
    "title":       { "type": ["string", "null"] },
    "servings":    { "type": ["integer", "null"], "minimum": 1 },
    "ingredients": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["name"],
        "additionalProperties": false,
        "properties": {
          "name":     { "type": "string" },
          "quantity": { "type": ["number", "null"] },
          "unit":     { "type": ["string", "null"] },
          "notes":    { "type": ["string", "null"] }
        }
      }
    },
    "steps":       { "type": "array", "items": { "type": "string" } },
    "notes":       { "type": ["string", "null"] },
    "confidence":  { "type": "number", "minimum": 0, "maximum": 1 },
    "needsReview": { "type": "boolean" }
  }
}
```

**Backend implementation**
- Set `loopComplete = true` on orchestrator state.
- Map input directly to `ImportedRecipeDto`.
- Return: `{ "status": "saved" }` (Claude sees this but loop exits immediately after).
- Does **not** persist to the database — caller still reviews and submits to `POST /api/recipes`.

---

## Security considerations

- **SSRF:** Block private IP ranges in `fetch_url_content`. Validate scheme is `http`/`https` before DNS lookup.
- **Content size:** Truncate fetched content at 50 000 chars to bound token spend.
- **Loop budget:** Hard cap at 10 iterations and 90 s to prevent runaway costs.
- **URL allowlist (future):** Not in scope for F2. Could add `RecipeImportOptions.AllowedDomains` later.

---

## Acceptance criteria

1. `POST /api/recipes/import/url` with a valid recipe URL returns `200 OK` with a populated `ImportedRecipeDto`.
2. `title`, `ingredients`, and `steps` are non-empty for a well-formed recipe page.
3. Ingredient names are lowercase and trimmed; fractional quantities are parsed to decimals.
4. Sending an unreachable URL returns `422` (loop handles `is_error: true` from fetch).
5. Sending a malformed URL (e.g. `"sourceUrl": "not-a-url"`) returns `400`.
6. `POST /api/recipes/import` (text-based) still works and is unmodified.
7. Application logs show each tool call name and iteration number.
8. Unit tests cover the agent loop with a fake Claude client scripting:
   - Happy path: fetch → extract → normalize (parallel) → save.
   - Fetch failure: `is_error: true` on first fetch, success on retry, then extract → normalize → save.
   - Abandon path: two consecutive fetch failures, Claude calls `save_recipe` with `needsReview: true`.
   - Loop exceeded: fake client never calls `save_recipe`; orchestrator returns `504`.

---

## Out of scope

- Frontend changes (no Angular/React UI for the URL import input).
- Persisting the recipe automatically — user still reviews `ImportedRecipeDto` before calling `POST /api/recipes`.
- Rate limiting or per-user quotas on the new endpoint.
- `tool_choice: forced` — intentionally using `auto` (forced appears in F4).
- HTML-to-markdown conversion beyond simple tag stripping.

---

## Files to create

| Path | Purpose |
|---|---|
| `Backend/src/Recipes.Application/Recipes/ImportRecipeFromUrl/ImportRecipeFromUrlCommand.cs` | Command record `ImportRecipeFromUrlCommand(string SourceUrl)` |
| `Backend/src/Recipes.Application/Recipes/ImportRecipeFromUrl/ImportRecipeFromUrlHandler.cs` | MediatR handler; delegates to `IRecipeImportAgent` |
| `Backend/src/Recipes.Application/Recipes/ImportRecipeFromUrl/ImportRecipeFromUrlValidator.cs` | FluentValidation: URL required, absolute, http/https, max 2048 |
| `Backend/src/Recipes.Application/Recipes/ImportRecipeFromUrl/IRecipeImportAgent.cs` | Interface: `Task<ErrorOr<ImportedRecipeDto>> RunAsync(string sourceUrl, CancellationToken)` |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Agents/RecipeImportAgent.cs` | Agentic loop implementation |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Agents/RecipeImportTools.cs` | Tool schemas (static `JsonElement` constants) and tool result types |
| `Backend/src/Recipes.Infrastructure/AI/Claude/Models/ClaudeToolModels.cs` | New model records: `ClaudeToolDefinition`, `ClaudeToolUseBlock`, `ClaudeToolResultBlock`, extended request/response with tool fields |
| `Backend/Docs/CCAF/F2-agentic-url-import.md` | CCAF documentation (subtopics → code locations, design decisions) |

## Files to modify

| Path | Change |
|---|---|
| `Backend/src/Recipes.Api/Endpoints/RecipesEndpoints.cs` | Add `POST /api/recipes/import/url` route |
| `Backend/src/Recipes.Infrastructure/DependencyInjection.cs` | Register `RecipeImportAgent` as `IRecipeImportAgent`; register `"RecipeUrlFetcher"` named `HttpClient` |

## Files not modified

- `ClaudeRecipeImportClient.cs` — preserved exactly as-is
- `ClaudeMessagesRequest.cs` / `ClaudeMessagesResponse.cs` — new tool models live in `ClaudeToolModels.cs`
- `RecipeImportOrchestrator.cs` — unchanged
- Any existing AI service, prompt file, or schema file
