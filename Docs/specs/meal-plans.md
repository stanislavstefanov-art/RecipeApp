# Spec: meal plans (list, create, AI suggest + accept, detail)

Framework-agnostic specification for meal plan management.
Both `/Frontend` (React) and `/FrontendAngular` (Angular) must satisfy this spec.

## Endpoints

### List meal plans
- Method: `GET /api/meal-plans`
- Definition: `Backend/src/Recipes.Api/Endpoints/MealPlansEndpoints.cs:103-107`
- Response `200 OK`: array of `{ id: string; name: string; householdId: string; householdName: string; entryCount: number }`

### Create meal plan
- Method: `POST /api/meal-plans`
- Definition: `Backend/src/Recipes.Api/Endpoints/MealPlansEndpoints.cs:22-26`
- Request: `{ name: string; householdId: string }` (name required, max 200; householdId required)
- Response `201 Created`: `{ id: string; name: string }`

### Get meal plan detail
- Method: `GET /api/meal-plans/{id}`
- Definition: `Backend/src/Recipes.Api/Endpoints/MealPlansEndpoints.cs:28-32`
- Response `200 OK`:
  ```
  {
    id: string
    name: string
    householdId: string
    householdName: string
    entries: Array<{
      id: string
      baseRecipeId: string
      baseRecipeName: string
      plannedDate: string       // ISO date "YYYY-MM-DD"
      mealType: number          // 1=Breakfast 2=Lunch 3=Dinner 4=Snack
      scope: number             // 1=Shared 2=SharedWithVariations 3=Individual
      assignments: Array<{
        personId: string
        personName: string
        assignedRecipeId: string
        assignedRecipeName: string
        recipeVariationId: string | null
        recipeVariationName: string | null
        portionMultiplier: number
        notes: string | null
      }>
    }>
  }
  ```
- Response `404 Not Found`: `ProblemDetails`

### Suggest meal plan (AI)
- Method: `POST /api/meal-plans/suggest`
- Definition: `Backend/src/Recipes.Api/Endpoints/MealPlansEndpoints.cs:67-79`
- Request: `{ name: string; householdId: string; startDate: string; numberOfDays: number; mealTypes: number[] }`
  - name required, max 200; householdId required; numberOfDays 1–31; mealTypes non-empty
- Response `200 OK`:
  ```
  {
    name: string
    entries: Array<{
      baseRecipeId: string
      plannedDate: string
      mealType: number
      scope: number
      assignments: Array<{
        personId: string
        assignedRecipeId: string
        recipeVariationId: string | null
        portionMultiplier: number
        notes: string | null
      }>
    }>
    confidence: number
    needsReview: boolean
    notes: string | null
  }
  ```
- `400 ProblemDetails` on validation failure

### Accept meal plan suggestion
- Method: `POST /api/meal-plans/accept-suggestion`
- Definition: `Backend/src/Recipes.Api/Endpoints/MealPlansEndpoints.cs:81-101`
- Request mirrors the suggestion DTO: `{ name, householdId, entries[] }` (IDs only — same shape as suggest response)
- Response `201 Created`: `{ mealPlanId: string; name: string }`

### Supporting endpoints (used for select/lookup)
- `GET /api/households` — for household select on create and suggest pages
- `GET /api/recipes` — for recipe name lookup in suggest preview (suggestion returns IDs only)

## User-visible behavior

- The list page (`/meal-plans`) shows all meal plans with name, household name, and entry count. Empty state: "No meal plans yet." Loading and error states required.
- The list page has a **New meal plan** link to `/meal-plans/new` and a **Suggest with AI** link to `/meal-plans/suggest`.
- The create page (`/meal-plans/new`) has a **Name** field (required, max 200) and a **Household** select populated from `GET /api/households`. Select is disabled while households are loading. On `201`, navigates to `/meal-plans/{id}`.
- The suggest page (`/meal-plans/suggest`) has: **Name** field, **Household** select (from `GET /api/households`), **Start date** date input (required), **Number of days** number input (required, 1–31), **Meal types** checkboxes (Breakfast, Lunch, Dinner, Snack — at least one required). On submit, calls `POST /api/meal-plans/suggest`. While loading, button shows "Suggesting…". On success, shows a preview panel with the suggestion entries: date, meal type label, recipe name (looked up from `GET /api/recipes`), scope label. Shows `needsReview` advisory and `notes` if present. Preview has an **Accept** button. On Accept, calls `POST /api/meal-plans/accept-suggestion` with the full suggestion payload. On `201`, navigates to `/meal-plans/{mealPlanId}`.
- The detail page (`/meal-plans/:id`) shows name, household name, and entries grouped by date. Each entry shows: meal type label, recipe name, scope label, and each assignment's person name and assigned recipe name. `404` shows not-found state.

## Enum labels (hard-coded in frontend)

MealType: 1=Breakfast, 2=Lunch, 3=Dinner, 4=Snack
MealScope: 1=Shared, 2=Shared with variations, 3=Individual

## Acceptance checklist

- [ ] `/meal-plans` shows list with name, household name, entry count, or empty state.
- [ ] "New meal plan" navigates to `/meal-plans/new`.
- [ ] "Suggest with AI" navigates to `/meal-plans/suggest`.
- [ ] Blank name on create shows inline error and issues no request.
- [ ] On `201` from create, navigates to `/meal-plans/{id}`.
- [ ] Suggest: blank name / no meal type selected / no household shows inline errors and issues no request.
- [ ] Suggest: on `200`, preview shows entries with date, meal type label, recipe name, scope label.
- [ ] Suggest: `needsReview: true` shows advisory.
- [ ] Suggest: `notes` is rendered.
- [ ] Accept: on `201`, navigates to `/meal-plans/{mealPlanId}`.
- [ ] Accept: on error, shows inline error near the Accept button.
- [ ] `/meal-plans/:id` shows name, household name, and entries with meal type, recipe name, scope.
- [ ] `/meal-plans/:id` shows `404` not-found state.

## Out of scope

- Adding entries to a meal plan manually (POST /api/meal-plans/{id}/entries).
- Shopping list generation from a meal plan.
- Updating person assignments (PUT .../assignments).
- Regenerating shopping lists.
- Recipe variation display in detail page assignments.

## Parity notes

**New feature — no React implementation exists yet.** Angular is the reference implementation.

## Parity reference

React implementation: none yet.
