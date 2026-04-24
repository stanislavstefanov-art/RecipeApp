# Spec: recipe import from unstructured text

Framework-agnostic specification for extracting a recipe from pasted text via AI.
Both `/Frontend` (React) and `/FrontendAngular` (Angular) must satisfy this spec.
Implementation details belong in the respective rules files.

## Endpoint

- Method: `POST /api/recipes/import`
- Definition: `Backend/src/Recipes.Api/Endpoints/RecipesEndpoints.cs:31-35`
- Request body:
  ```
  { text: string }   // required, minimum 10 characters
  ```
- Response `200 OK`:
  ```
  {
    title?: string
    servings?: number
    ingredients: Array<{ name: string; quantity?: string; unit?: string; notes?: string }>
    steps: string[]
    notes?: string
    confidence: number    // 0–1, not shown in UI
    needsReview: boolean
  }
  ```
- Handler: `Backend/src/Recipes.Application/Recipes/ImportRecipeFromText/ImportRecipeFromTextHandler.cs`
- Validation: `ImportRecipeFromTextValidator.cs` — `text` required, min 10 characters.
- On validation failure: `400 Bad Request` with `ProblemDetails`.
- Other failures: `ProblemDetails` via `ErrorOrExtensions.ToHttpResult()`.
- The endpoint does not save anything — it returns an extraction preview only.

## User-visible behavior

- The import feature lives on a dedicated page at `/recipes/import`.
- The recipes list page has an "Import recipe" link that navigates to `/recipes/import`.
- The page shows a single textarea labelled **Recipe text** and an **Extract** button.
- Submitting with fewer than 10 characters (or blank) shows a client-side inline error and issues no request.
- Clicking **Extract** with valid text sends `POST /api/recipes/import`.
- While the request is in flight the button is disabled and shows a loading label.
- On `200 OK`, the extraction result is displayed below the form in a preview panel:
  - **Title** (if present).
  - **Servings** (if present).
  - **Ingredients** list: each row shows name, and quantity + unit where available; notes where present.
  - **Steps** list: numbered, one per step.
  - **Notes** (if present).
  - `needsReview: true` triggers a visible advisory note at the top of the preview.
  - `confidence` is not shown.
- On `400` or `5xx`, a non-empty error message is displayed near the button. The textarea retains its content. The button re-enables.
- The form is not reset after a successful extraction — the user can re-submit with edited text.
- The preview does not have a "Save" or "Create recipe" action in this slice — it is display-only.

## Acceptance checklist

- [ ] `/recipes/import` is a reachable route.
- [ ] The recipes list page has a visible "Import recipe" link or button leading to `/recipes/import`.
- [ ] Submitting fewer than 10 characters shows a client-side inline error and issues no request.
- [ ] Submitting valid text sends `POST /api/recipes/import` with the correct body.
- [ ] While the request is in flight, the button is disabled or shows a loading state.
- [ ] On `200 OK`, title, servings, ingredients, steps, and notes are rendered where present.
- [ ] `needsReview: true` renders a visible advisory note.
- [ ] On `400` or `5xx`, a visible error message is displayed and the textarea retains its content.

## Out of scope

- Saving / creating a recipe from the extracted preview (follow-up slice).
- Editing the extracted fields in-place before saving.
- File upload (PDF, image) — text input only.
- Showing the `confidence` score to the user.
- History of past imports.

## Parity notes

**New feature — no React implementation exists yet.** Angular is the reference implementation.

## Parity reference

React implementation: none yet.
