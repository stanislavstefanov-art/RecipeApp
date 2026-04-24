# Spec: add ingredient

Framework-agnostic specification for adding an ingredient to an
existing recipe. Both `/Frontend` (React) and `/FrontendAngular`
(Angular) must satisfy this spec. Implementation details belong in the
respective rules files.

## Endpoint

- Method: `POST /api/recipes/{id}/ingredients`
- Definition: `Backend/src/Recipes.Api/Endpoints/RecipesEndpoints.cs:60-66`
- Request body: `AddIngredientToRecipeRequest { name: string; quantity: decimal; unit: string }`
  (`Backend/src/Recipes.Application/Recipes/AddIngredientToRecipe/AddIngredientToRecipeRequest.cs`)
- Server-side validation
  (`Backend/src/Recipes.Application/Recipes/AddIngredientToRecipe/AddIngredientToRecipeValidator.cs:7-22`):
  - `name` required, max length 200
  - `unit` required, max length 50
  - `quantity` greater than 0
  - `recipeId` non-empty
- Response `204 No Content` on success.
- Response `400 Bad Request`: `ProblemDetails` with validation errors
  (via the MediatR validation behavior).
- Response `404 Not Found`: `ProblemDetails` when no recipe has the id
  (handler looks up the aggregate before adding).
- Other failures: `ProblemDetails` via
  `ErrorOrExtensions.ToHttpResult()`.

## User-visible behavior

- The "Add ingredient" form lives on the recipe details page
  (`/recipes/{id}`). No separate route.
- The form has three fields: name (text), quantity (number), and
  unit (text). Initial values: name empty, quantity `1`, unit empty.
- Client-side validation mirrors the server:
  - `name` required, max length 200
  - `unit` required, max length 50
  - `quantity` must be greater than 0
- On submit:
  - If client validation fails, show inline errors under the failing
    fields and issue **no** request.
  - If client validation passes, `POST /api/recipes/{id}/ingredients`
    with `{ name, quantity, unit }`.
- On `204 No Content`:
  - The ingredients list on the details page reflects the new row
    (the details resource is reloaded or the cache is updated).
  - The form resets to its initial values so the user can add
    another ingredient.
- On `400` or `5xx`, display a non-empty error message near the form.
  The form stays interactive and the entered values are preserved.
- The submit button is disabled (or shows an adding state) while the
  request is in flight.

## Acceptance checklist

- [ ] The details page shows the add-ingredient form with fields
      name, quantity, and unit.
- [ ] Submitting with an empty name shows an inline error and issues
      no request.
- [ ] Submitting with an empty unit shows an inline error and issues
      no request.
- [ ] Submitting with quantity ≤ 0 shows an inline error and issues
      no request.
- [ ] On a successful `204`, the new ingredient appears in the
      ingredients list without a manual page refresh.
- [ ] On a successful `204`, the form resets to its initial values.
- [ ] On a `400` response, a visible error message is rendered and
      the entered values are preserved.
- [ ] On a `500` response, a visible error message is rendered and
      the form remains interactive.
- [ ] While the request is in flight, the submit button is disabled
      or shows an adding state.

## Out of scope

- Editing or removing an ingredient (not modeled in the backend
  today).
- Reordering ingredients.
- Autocomplete on ingredient name or unit.
- Deleting the recipe (slice 7).
- Toast notifications (same known divergence as the update-recipe-name
  slice — Angular has no toast system yet).

## Parity notes

**Known divergence:** the React implementation at
`Frontend/src/features/recipes/components/AddIngredientForm.tsx`
pushes success and error toasts via `useToastStore`. Angular surfaces
errors inline and communicates success by refreshing the ingredients
list and resetting the form. Revisit when Angular grows a toast
system.

## Parity reference

React implementation:

- Page: `Frontend/src/pages/recipes/RecipeDetailsPage.tsx` (renders
  `AddIngredientForm` inline)
- Form: `Frontend/src/features/recipes/components/AddIngredientForm.tsx`
- Hook: `Frontend/src/features/recipes/hooks/useAddIngredient.ts`
- API: `Frontend/src/api/recipes.ts` (`addIngredient`)
- Zod schema: `Frontend/src/features/recipes/schemas.ts`
  (`addIngredientSchema`)
