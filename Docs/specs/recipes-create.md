# Spec: recipes create

Framework-agnostic specification for creating a recipe. Both
`/Frontend` (React) and `/FrontendAngular` (Angular) must satisfy this
spec. Implementation details belong in the respective rules files.

## Endpoint

- Method: `POST /api/recipes`
- Definition: `Backend/src/Recipes.Api/Endpoints/RecipesEndpoints.cs:24-28`
- Request body: `CreateRecipeRequest { name: string }`
  (`Backend/src/Recipes.Api/Endpoints/RecipesEndpoints.cs:124`)
- Server-side validation (`CreateRecipeCommandValidator.cs:7-12`):
  - `name` required (non-empty)
  - `name` max length 200
- Response `201 Created`:
  - `Location` header: `/api/recipes/{id}`
  - Body: `CreateRecipeResponse { id: Guid }`
    (`Backend/src/Recipes.Application/Recipes/CreateRecipe/CreateRecipeCommand.cs:8`)
- Response `400 Bad Request`: `ProblemDetails` with validation errors
  (via the MediatR validation behavior).
- Other failures: `ProblemDetails` via
  `ErrorOrExtensions.ToHttpResult()`.

## User-visible behavior

- Route `/recipes/new` renders a form with a single "Name" text input
  and a "Create" submit button. No "Cancel" button (parity with React:
  users navigate away via the back button or the list page).
- Client-side validation mirrors the server:
  - `name` required (non-empty)
  - `name` max length 200
- On submit:
  - If client validation fails, show an inline error under the field
    and issue **no** request.
  - If client validation passes, `POST /api/recipes` with `{ name }`.
- On `201 Created`, navigate to `/recipes/{id}` using the `id` from
  the response body. That route is 404 until slice 3 (parity).
- On `400` or `5xx`, display a non-empty error message near the form.
  The form stays interactive and the name field is preserved.
- The "New recipe" button on `/recipes` links to `/recipes/new`.

## Acceptance checklist

- [ ] `/recipes/new` renders the form with exactly one Name field and
      one Create button.
- [ ] Submitting an empty name shows an inline error and issues no
      network request.
- [ ] Submitting a 201-character name shows an inline error and issues
      no network request.
- [ ] On a successful 201 response, the browser navigates to
      `/recipes/{id}` using the returned id.
- [ ] On a 400 response from the server, a visible error message is
      rendered and the name field retains its value.
- [ ] On a 500 response, a visible error message is rendered and the
      form remains interactive.
- [ ] The "New recipe" button on `/recipes` navigates here.

## Out of scope

- Recipe details / viewing (slice 3).
- Editing recipe name after creation (slice 4).
- Ingredients and steps (slices 5–6).
- Optimistic updates or list-cache invalidation on success.
- Keyboard shortcuts, autosave, dirty-form warnings, or confirmation
  dialogs.

## Parity notes

**Known divergence:** the React implementation at
`Frontend/src/pages/recipes/CreateRecipePage.tsx` currently ignores
server errors on submit — it awaits `mutateAsync` without a `catch`,
so a failed request silently leaves the user on the form with no
feedback. Angular must render server errors (see acceptance checklist
rows 5–6). React's behavior is a parity gap to fix in a follow-up.

## Parity reference

React implementation:

- Page: `Frontend/src/pages/recipes/CreateRecipePage.tsx`
- Hook: `Frontend/src/features/recipes/hooks/useCreateRecipe.ts`
- API: `Frontend/src/api/recipes.ts` (`createRecipe`)
- Zod schema: `Frontend/src/features/recipes/schemas.ts`
  (`createRecipeSchema`)
- Route: `Frontend/src/app/router.tsx`
