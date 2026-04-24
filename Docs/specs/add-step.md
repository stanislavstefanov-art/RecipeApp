# Spec: add step

Framework-agnostic specification for adding a step to an existing
recipe. Both `/Frontend` (React) and `/FrontendAngular` (Angular) must
satisfy this spec. Implementation details belong in the respective
rules files.

## Endpoint

- Method: `POST /api/recipes/{id}/steps`
- Definition: `Backend/src/Recipes.Api/Endpoints/RecipesEndpoints.cs:68-74`
- Request body: `AddStepToRecipeRequest { instruction: string }`
  (`Backend/src/Recipes.Application/Recipes/AddStepToRecipe/AddStepToRecipeRequest.cs`)
- Server-side validation
  (`Backend/src/Recipes.Application/Recipes/AddStepToRecipe/AddStepToRecipeValidator.cs:7-14`):
  - `instruction` required, max length 1000
  - `recipeId` non-empty
- Response `204 No Content` on success. Step `order` is assigned by
  the domain (`Recipe.AddStep` uses `_steps.Count + 1`); the frontend
  does not choose or send it.
- Response `400 Bad Request`: `ProblemDetails` with validation errors
  (via the MediatR validation behavior).
- Response `404 Not Found`: `ProblemDetails` when no recipe has the
  id.
- Other failures: `ProblemDetails` via
  `ErrorOrExtensions.ToHttpResult()`.

## User-visible behavior

- The "Add step" form lives on the recipe details page
  (`/recipes/{id}`). No separate route.
- The form has a single multi-line `instruction` field. Initial
  value: empty.
- Client-side validation mirrors the server:
  - `instruction` required (non-empty)
  - `instruction` max length 1000
- On submit:
  - If client validation fails, show an inline error under the field
    and issue **no** request.
  - If client validation passes, `POST /api/recipes/{id}/steps` with
    `{ instruction }`.
- On `204 No Content`:
  - The steps list on the details page shows the new step with the
    next `order` value.
  - The form resets to its initial empty value.
- On `400` or `5xx`, display a non-empty error message near the form.
  The form stays interactive and the entered instruction is
  preserved.
- The submit button is disabled (or shows an adding state) while the
  request is in flight.

## Acceptance checklist

- [ ] The details page shows the add-step form with a multi-line
      instruction field.
- [ ] Submitting an empty instruction shows an inline error and
      issues no request.
- [ ] Submitting a 1001-character instruction shows an inline error
      and issues no request.
- [ ] On a successful `204`, the new step appears in the steps list
      with the next order number, without a manual page refresh.
- [ ] On a successful `204`, the form resets to empty.
- [ ] On a `400` response, a visible error message is rendered and
      the entered instruction is preserved.
- [ ] On a `500` response, a visible error message is rendered and
      the form remains interactive.
- [ ] While the request is in flight, the submit button is disabled
      or shows an adding state.

## Out of scope

- Editing or removing a step (not modeled in the backend today).
- Reordering steps.
- Deleting the recipe (slice 7).
- Toast notifications (same known divergence as update-recipe-name
  and add-ingredient).

## Parity notes

**Known divergence:** the React implementation at
`Frontend/src/features/recipes/components/AddStepForm.tsx` pushes
success and error toasts via `useToastStore`. Angular surfaces errors
inline and communicates success by refreshing the steps list and
resetting the form.

Prior to 2026-04-24 this endpoint did not exist on the backend —
React's `addStep` call silently 404-ed and the user saw the generic
error toast. The backend `POST /api/recipes/{id}/steps` slice was
added in the same feature-branch cycle as this spec.

## Parity reference

React implementation:

- Page: `Frontend/src/pages/recipes/RecipeDetailsPage.tsx` (renders
  `AddStepForm` inline)
- Form: `Frontend/src/features/recipes/components/AddStepForm.tsx`
- Hook: `Frontend/src/features/recipes/hooks/useAddStep.ts`
- API: `Frontend/src/api/recipes.ts` (`addStep`)
- Zod schema: `Frontend/src/features/recipes/schemas.ts`
  (`addStepSchema`)
