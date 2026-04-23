# Spec: recipes list

Framework-agnostic specification of the recipes-list slice. Both
`/Frontend` (React) and `/FrontendAngular` (Angular) must satisfy this
spec. Implementation details belong in the respective rules files, not
here.

## Endpoint

- Method: `GET /api/recipes`
- Definition: `Backend/src/Recipes.Api/Endpoints/RecipesEndpoints.cs:42-46`
- Request: none (no query params, no body)
- Response `200 OK`: `RecipeListItemDto[]` — array of `{ id: Guid, name: string }`
  - Type: `Backend/src/Recipes.Application/Recipes/ListRecipes/ListRecipesQuery.cs:8`
- Response on failure: `ProblemDetails` via the standard
  `ErrorOrExtensions.ToHttpResult()` mapping.

## User-visible behavior

- On route entry (`/recipes`), fetch the list from the endpoint above.
- Render one of four states, never more than one at a time:
  - **Loading** — while the request is in flight on first load.
  - **Error** — when the request fails. Show the error message.
  - **Empty** — on a successful response with zero items. Prompt the
    user to create their first recipe.
  - **Success** — a grid of recipe cards, each showing the recipe name.
- Each card is a link to the recipe-details route (`/recipes/{id}`).
- The page provides a primary action to create a new recipe, linking to
  `/recipes/new`.
- Links to `/recipes/{id}` and `/recipes/new` are rendered even though
  those routes are not yet implemented. Matches current React behavior;
  removes dead-link drift between apps when later slices land.

## Acceptance checklist

A reviewer can walk this list against either app:

- [ ] Navigating to `/recipes` triggers a single `GET /api/recipes`.
- [ ] While the request is pending, only the loading state is visible.
- [ ] When the request fails, only the error state is visible and it
      shows a non-empty message.
- [ ] When the response is `[]`, only the empty state is visible.
- [ ] When the response has items, each item renders once, showing its
      `name`.
- [ ] Each rendered item is an accessible link whose target is
      `/recipes/{id}`.
- [ ] A visible primary action links to `/recipes/new`.
- [ ] No duplicate requests are issued on re-render of the same page.

## Out of scope for this slice

- Pagination, filtering, sorting, or search.
- Recipe creation (slice 2).
- Recipe details page (slice 3).
- Client-side runtime validation of the response shape (React uses Zod;
  Angular currently does not. Deliberate asymmetry; revisit as an ADR if
  it causes incidents.)
- Optimistic updates, prefetching, or cache invalidation across slices.

## Parity reference

React implementation (for lookup, not imitation):

- Page: `Frontend/src/pages/recipes/RecipesPage.tsx`
- Hook: `Frontend/src/features/recipes/hooks/useRecipes.ts`
- API client: `Frontend/src/api/recipes.ts`
- DTO schema: `Frontend/src/features/recipes/schemas.ts`
- Route registration: `Frontend/src/app/router.tsx`
