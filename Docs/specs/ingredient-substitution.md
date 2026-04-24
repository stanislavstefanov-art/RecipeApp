# Spec: ingredient substitution suggestions

Framework-agnostic specification for suggesting ingredient substitutes via AI.
Both `/Frontend` (React) and `/FrontendAngular` (Angular) must satisfy this spec.
Implementation details belong in the respective rules files.

## Endpoint

- Method: `POST /api/recipes/suggest-substitutions`
- Definition: `Backend/src/Recipes.Api/Endpoints/RecipesEndpoints.cs:83-93`
- Request body:
  ```
  {
    ingredientName: string   // required, max 200 characters
    recipeContext?: string   // optional, max 2000 characters
    dietaryGoal?: string     // optional, max 200 characters
  }
  ```
- Response `200 OK`:
  ```
  {
    originalIngredient: string
    substitutes: Array<{
      name: string
      reason: string
      quantityAdjustment?: string
      isDirectReplacement: boolean
    }>
    confidence: number        // 0–1 double
    needsReview: boolean
    notes?: string
  }
  ```
- Handler: `Backend/src/Recipes.Application/Recipes/SuggestIngredientSubstitutions/SuggestIngredientSubstitutionsHandler.cs`
- Validation: `SuggestIngredientSubstitutionsValidator.cs` — `ingredientName` required/max 200, `recipeContext` max 2000, `dietaryGoal` max 200.
- On validation failure: `400 Bad Request` with `ProblemDetails`.
- Other failures: `ProblemDetails` via `ErrorOrExtensions.ToHttpResult()`.
- No recipe ID required — endpoint is not recipe-scoped.

## User-visible behavior

- The substitution form lives on the recipe details page (`/recipes/{id}`) in its own section.
- The form has three fields:
  - **Ingredient name** (required text input, max 200 characters).
  - **Recipe context** (optional textarea, max 2000 characters) — hint text "e.g. chocolate cake".
  - **Dietary goal** (optional text input, max 200 characters) — hint text "e.g. vegan, low-fat".
- Clicking **Find substitutes** with a blank ingredient name shows a client-side inline validation error and issues no request.
- Clicking **Find substitutes** with a valid ingredient name sends `POST /api/recipes/suggest-substitutions`.
- While the request is in flight the button is disabled and shows a loading label.
- On `200 OK`, the substitution results replace any previous results in the same section:
  - The original ingredient name is displayed.
  - Each substitute shows: name, reason, and `quantityAdjustment` if present.
  - A badge or label distinguishes direct replacements from partial ones.
  - `needsReview: true` triggers a visible advisory note.
  - `notes` (if present) is displayed below the substitutes list.
  - `confidence` is not displayed (internal signal only).
- On `400` or `5xx`, a non-empty error message is displayed near the button. Previous results (if any) remain visible. The button re-enables.
- The form is not reset after a successful response — the user can refine and re-submit.

## Acceptance checklist

- [ ] The details page shows a "Suggest substitutions" section with an Ingredient name field, Recipe context field, and Dietary goal field.
- [ ] Submitting with an empty ingredient name shows a client-side inline error and issues no request.
- [ ] Submitting with a valid name (and optional fields) sends `POST /api/recipes/suggest-substitutions` with the correct body.
- [ ] While the request is in flight, the button is disabled or shows a loading state.
- [ ] On `200 OK`, the substitutes list renders with name, reason, and quantity adjustment (where present).
- [ ] Direct vs. partial replacements are visually distinguished.
- [ ] `needsReview: true` renders a visible advisory note.
- [ ] `notes` (when non-null) is rendered below the list.
- [ ] On `400` or `5xx`, a visible error message is rendered near the button and the form stays interactive.

## Out of scope

- Ingredient-name max-length client-side validation (200 chars) — omit to keep the form lean; the server returns 400 on violation.
- Persisting substitution results to the recipe.
- Showing the `confidence` value to the user.
- Substitution history or caching.
- Triggering substitution from an ingredient row click (requires more UI design).

## Parity notes

**New feature — no React implementation exists yet.** Angular is the reference implementation. React should follow in a follow-up.

## Parity reference

React implementation: none yet.
