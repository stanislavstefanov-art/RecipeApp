# Spec: recipe details

Framework-agnostic specification for viewing a single recipe. Both
`/Frontend` (React) and `/FrontendAngular` (Angular) must satisfy this
spec. Implementation details belong in the respective rules files.

## Endpoint

- Method: `GET /api/recipes/{id}`
- Definition: `Backend/src/Recipes.Api/Endpoints/RecipesEndpoints.cs:36-40`
- Response `200 OK`: `RecipeDto`
  (`Backend/src/Recipes.Application/Recipes/GetRecipe/GetRecipeQuery.cs:8-16`)
  - `id: Guid`
  - `name: string`
  - `ingredients: IngredientDto[]` where
    `IngredientDto = { name: string; quantity: decimal; unit: string }`
  - `steps: RecipeStepDto[]` where
    `RecipeStepDto = { order: int; instruction: string }`
  - Handler:
    `Backend/src/Recipes.Application/Recipes/GetRecipe/GetRecipeHandler.cs`
- Response `404 Not Found`: `ProblemDetails` with error code
  `Recipe.NotFound` when no recipe has the given id
  (`GetRecipeHandler.cs:23-26`).
- Other failures: `ProblemDetails` via
  `ErrorOrExtensions.ToHttpResult()`.

## User-visible behavior

- Route: `/recipes/{id}`.
- The page shows the recipe name as a heading, the ingredients list,
  and the steps list.
- A back link ("← Back to recipes" or similar) returns to `/recipes`.
- Loading: while the request is in flight, a non-blank loading state
  is visible (inline text or skeleton — framework's choice).
- Error (non-404): render a visible error message with the server's
  detail when available. The back link remains usable.
- Not found (404): render a distinct "Recipe not found" state with
  the back link. This is not the same as the generic error state —
  a 404 is a recognized terminal state, not a failure.
- Empty ingredients / empty steps: each list renders its own empty
  hint ("No ingredients" / "No steps"). Both can be empty on the
  same page.
- Ingredient rows render `{quantity} {unit} {name}` or an equivalent
  arrangement that keeps all three fields visible.
- Step rows render in `order` order, with the step number visible
  next to the instruction.

## Acceptance checklist

- [ ] Visiting `/recipes/{known-id}` renders the recipe name,
      ingredients, and steps from the response.
- [ ] Loading state is visible before the first response resolves.
- [ ] `/recipes/{unknown-id}` renders the "Recipe not found" state,
      not the generic error state.
- [ ] A 500 response renders a visible error message and keeps the
      back link usable.
- [ ] An empty `ingredients` array renders the "No ingredients" hint;
      an empty `steps` array renders the "No steps" hint.
- [ ] Step numbers are visible and ordered by `order`.
- [ ] The back link navigates to `/recipes`.

## Out of scope

- Updating the recipe name (slice 4).
- Adding ingredients (slice 5).
- Adding steps (slice 6).
- Deleting the recipe (slice 7).
- Recipe variations (`RecipeDto` does not include them and the
  `GetRecipe` handler does not populate them today).
- Ingredient / step reordering or editing.
- Caching, prefetching, or optimistic updates.

## Parity notes

**Known divergence:** the React implementation at
`Frontend/src/pages/recipes/RecipeDetailsPage.tsx` already renders the
slice 4–7 affordances (update name form, add ingredient form, add step
form, delete button). Angular slice 3 is **read-only** to keep the
slice order strict; slices 4–7 will add those in separate specs. When
Angular catches up, this page must render the same affordances.

## Parity reference

React implementation:

- Page: `Frontend/src/pages/recipes/RecipeDetailsPage.tsx`
- Hook: `Frontend/src/features/recipes/hooks/useRecipe.ts`
- API: `Frontend/src/api/recipes.ts` (`getRecipe`)
- Zod schema: `Frontend/src/features/recipes/schemas.ts`
  (`recipeDetailsSchema`, `recipeIngredientSchema`, `recipeStepSchema`)
- Route: `Frontend/src/app/router.tsx` (`recipes/:recipeId`)
