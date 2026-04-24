# Spec: delete recipe

Framework-agnostic specification for deleting an existing recipe.
Both `/Frontend` (React) and `/FrontendAngular` (Angular) must satisfy
this spec. Implementation details belong in the respective rules
files.

## Endpoint

- Method: `DELETE /api/recipes/{id}`
- Definition: `Backend/src/Recipes.Api/Endpoints/RecipesEndpoints.cs:76-80`
- No request body.
- Handler:
  `Backend/src/Recipes.Application/Recipes/DeleteRecipe/DeleteRecipeHandler.cs`.
- Response `204 No Content` on success.
- Response `404 Not Found`: `ProblemDetails` with error code
  `Recipe.NotFound` when no recipe has the id (`DeleteRecipeHandler.cs:23-26`).
- Other failures: `ProblemDetails` via
  `ErrorOrExtensions.ToHttpResult()`.
- Cascade: deleting a `Recipe` aggregate cascade-deletes its
  ingredients and steps via the EF Core configuration in
  `Backend/src/Recipes.Infrastructure/Persistence/Configurations/RecipeConfiguration.cs`.

## User-visible behavior

- The "Delete recipe" button lives on the recipe details page
  (`/recipes/{id}`). No separate route.
- Clicking the button opens a native browser confirm dialog asking
  the user to confirm the deletion.
- If the user cancels the confirm dialog, nothing happens and no
  request is issued.
- If the user confirms, `DELETE /api/recipes/{id}` is issued.
- On `204 No Content`, the browser navigates to `/recipes`. The
  recipe no longer appears in the list on that page.
- On `404`, `4xx`, or `5xx`, display a non-empty error message near
  the button. The user stays on the details page. The button stays
  interactive so they can retry.
- While the request is in flight, the button is disabled (or shows
  a deleting state).

## Acceptance checklist

- [ ] The details page shows a "Delete recipe" button.
- [ ] Clicking the button opens a confirm dialog.
- [ ] Dismissing the confirm dialog issues no network request and
      keeps the user on the details page.
- [ ] On confirmed `204`, the browser navigates to `/recipes`.
- [ ] On a `404` or `5xx` response, a visible error message is
      rendered and the user stays on the details page.
- [ ] While the request is in flight, the delete button is disabled
      or shows a deleting state.

## Out of scope

- A custom (non-native) confirmation dialog.
- Undo / recovery after delete.
- Soft-delete semantics (the backend hard-deletes).
- Bulk delete from the list page.
- Toast notifications (same known divergence pattern — Angular has no
  toast system yet).

## Parity notes

**Known divergence:** the React implementation at
`Frontend/src/features/recipes/components/DeleteRecipeButton.tsx`
does not render a visible error on failure — it `await`s
`mutateAsync` without a `catch`, so a failed DELETE silently leaves
the user on the details page with no feedback (the button just
re-enables). Angular must render server errors near the button (see
acceptance checklist rows 5–6). This mirrors the known parity gap
for the recipes-create slice and should be fixed on the React side
in a follow-up.

## Parity reference

React implementation:

- Page: `Frontend/src/pages/recipes/RecipeDetailsPage.tsx` (renders
  `DeleteRecipeButton` next to the header)
- Button: `Frontend/src/features/recipes/components/DeleteRecipeButton.tsx`
- Hook: `Frontend/src/features/recipes/hooks/useDeleteRecipe.ts`
- API: `Frontend/src/api/recipes.ts` (`deleteRecipe`)
