# Spec: update recipe name

Framework-agnostic specification for updating the name of an existing
recipe. Both `/Frontend` (React) and `/FrontendAngular` (Angular) must
satisfy this spec. Implementation details belong in the respective
rules files.

## Endpoint

- Method: `PUT /api/recipes/{id}`
- Definition: `Backend/src/Recipes.Api/Endpoints/RecipesEndpoints.cs:54-58`
- Request body: `UpdateRecipeRequest { name: string }`
  (`RecipesEndpoints.cs:128`)
- Server-side validation
  (`Backend/src/Recipes.Application/Recipes/UpdateRecipe/UpdateRecipeCommandValidator.cs:7-12`):
  - `name` required (non-empty)
  - `name` max length 200
- Response `204 No Content` on success.
- Response `400 Bad Request`: `ProblemDetails` with validation errors
  (via the MediatR validation behavior).
- Response `404 Not Found`: `ProblemDetails` when no recipe has the id
  (`UpdateRecipeHandler` — missing aggregate).
- Other failures: `ProblemDetails` via
  `ErrorOrExtensions.ToHttpResult()`.

## User-visible behavior

- The "Update recipe name" form lives on the recipe details page
  (`/recipes/{id}`). No separate route.
- The name field is pre-filled with the recipe's current name.
- Client-side validation mirrors the server:
  - `name` required (non-empty)
  - `name` max length 200
- On submit:
  - If client validation fails, show an inline error under the field
    and issue **no** request.
  - If client validation passes, `PUT /api/recipes/{id}` with
    `{ name }`.
- On `204 No Content`, the details view reflects the new name (the
  details resource is reloaded or the cached name is updated). The
  form remains visible and stays pre-filled with the saved value.
- On `400` or `5xx`, display a non-empty error message near the form.
  The form stays interactive and the name field is preserved.
- The submit button is disabled (or shows a saving state) while the
  request is in flight.

## Acceptance checklist

- [ ] The details page shows the update form pre-filled with the
      current name.
- [ ] Clearing the name and submitting shows an inline error and
      issues no network request.
- [ ] Submitting a 201-character name shows an inline error and
      issues no network request.
- [ ] On a successful `204`, the details view shows the new name
      without a manual page refresh.
- [ ] On a `400` response, a visible error message is rendered and
      the name field retains its value.
- [ ] On a `500` response, a visible error message is rendered and
      the form remains interactive.
- [ ] While the PUT is in flight, the submit button is disabled or
      shows a saving state.

## Out of scope

- Editing ingredients or steps (slices 5–6).
- Deleting the recipe (slice 7).
- Confirmation dialogs or "unsaved changes" warnings.
- Optimistic UI updates.
- Toast notifications (Angular has no toast system yet; React's
  success/error toasts are a parity gap called out below).

## Parity notes

**Known divergence:** the React implementation at
`Frontend/src/features/recipes/components/UpdateRecipeNameForm.tsx`
pushes success and error toasts via `useToastStore`. Angular has no
equivalent toast system today, so the Angular implementation must
surface errors inline near the form and communicate success by
refreshing the displayed name. If/when Angular adds a toast system,
this spec section should be revisited.

## Parity reference

React implementation:

- Page: `Frontend/src/pages/recipes/RecipeDetailsPage.tsx`
  (renders `UpdateRecipeNameForm` inline)
- Form: `Frontend/src/features/recipes/components/UpdateRecipeNameForm.tsx`
- Hook: `Frontend/src/features/recipes/hooks/useUpdateRecipe.ts`
- API: `Frontend/src/api/recipes.ts` (`updateRecipe`)
- Zod schema: `Frontend/src/features/recipes/schemas.ts`
  (`updateRecipeSchema`)
