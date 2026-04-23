---
paths: ["Frontend/**/*"]
---

# Claude Code Rules: React 19 Feature-Driven Architecture

> Applies to the **React** app at `/Frontend`. The sibling Angular app lives at
> `/FrontendAngular` — do not share source code between the two.

## Core Tech Stack
- **Framework:** React 19 + TypeScript (Strict Mode) + Vite
- **Styling:** Tailwind CSS
- **State (Server):** TanStack Query (Query/Mutation) — **No Redux**
- **State (UI):** Zustand stores
- **Forms:** React Hook Form + Zod for validation

## Project Structure (Feature-First)
Organize the codebase by domain features to ensure modularity and scalability.

- `src/features/[feature-name]/`: Feature-specific logic (e.g., `recipes`, `households`, `mealPlans`).
  - Contains its own `components/`, `hooks/`, and local `types/`.
- `src/components/ui/`: Global, reusable UI primitives (atomic components).
- `src/pages/`: Route entry points. Pages compose feature-specific components.
- `src/api/` or `src/services/`: Centralized typed API clients.
- `src/hooks/`: Global, generic utility hooks.

## Data & State Management
- **Server State:** Use TanStack Query for all server-side data fetching and caching.
- **Client State:** Use Zustand for global UI state; `useState` for local component state.
- **API Calls:** Always use a typed client/service. **Never fetch directly inside components.**
- **Typing:** Keep DTO types close to the feature. Shared DTOs go in `src/types/api.ts`.

## Implementation Rules
- **Component Evolution:** Follow the "Rule of Three." Build simple components first; extract shared UI only when repetitive patterns emerge.
- **Data Safety:** Use Zod schemas to validate API responses and form inputs.
- **UX States:** Every page and major feature component must explicitly handle **Loading**, **Error**, and **Empty** states.
- **Logic Extraction:** Move complex business logic or data transformations out of components and into custom hooks within the feature directory.

## Recipes slice order

When building the React frontend, start with:
1. recipes list
2. create recipe
3. recipe details
4. update recipe name
5. add ingredient
6. add step
7. delete recipe

Only move to persons / households / meal plans after the recipe slice is working end-to-end.

## Feature implementation preference

For each feature:
- define Zod schemas first
- define typed API client functions second
- define TanStack Query hooks third
- implement pages last

## Next slice order after recipes

After the recipes slice is working:
1. persons list + create + details
2. households list + create + details
3. add household members
4. only then move to meal plans

Households and persons provide the context required for realistic meal-plan UI.

## Meal plan slice order

After persons and households:
1. meal plans list
2. meal plan details
3. group entries by date
4. render base recipe, scope, assignments, variation names, and portions
5. keep meal plans read-only first
6. only then implement editing workflows

## Meal plan editing slice

After meal plan read-only pages:
1. add per-assignment Edit actions
2. use a modal for editing one assignment
3. fetch recipe options through feature hooks
4. update recipe variation options when assigned recipe changes
5. persist through the meal plan assignment update endpoint
6. refetch meal plan after mutation

## Meal plan suggestion slice

After meal plan editing:
1. add suggest meal plan form
2. persist suggestion request/result in Zustand for review flow
3. render suggestion entries in a review page
4. accept suggestion through backend mutation
5. redirect to saved meal plan details
6. keep suggestion review separate from saved meal plan UI

## Shopping list slice

After meal plan suggestion/acceptance:
1. implement shopping lists list + details
2. support generation from meal plan
3. support regeneration from meal plan
4. support pending/purchased transitions
5. use a modal for purchase-with-expense
6. keep shopping-list item source metadata visible

## Expenses slice

After shopping lists:
1. implement expenses list + create manual expense
2. implement monthly report page
3. show summary metrics and category breakdown
4. show insights from the expense insights endpoint
5. keep reporting read-only first

## Stabilization and UX pass

After the core feature slices are working:
1. replace raw enum numbers with readable labels
2. add a toast system for mutation feedback
3. use selectors instead of manual ID inputs
4. extract repeated page headers and loading buttons
5. keep loading, error, and empty states explicit everywhere
6. prefer incremental cleanup over big rewrites

## Mobile polish pass

Before any framework rewrite:
1. reduce horizontal padding on small screens
2. make modals full-height/full-width on phones, centered on desktop
3. prefer stacked actions on mobile
4. tighten card spacing on small screens
5. avoid manual ID entry where selectors can be used
6. fix invalid hook usage before styling changes