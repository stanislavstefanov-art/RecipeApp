# Spec: shopping lists (list, create, detail)

Framework-agnostic specification for shopping list management.
Both `/Frontend` (React) and `/FrontendAngular` (Angular) must satisfy this spec.

## Endpoints

### List shopping lists
- Method: `GET /api/shopping-lists`
- Definition: `Backend/src/Recipes.Api/Endpoints/ShoppingListsEndpoints.cs:28-32`
- Response `200 OK`: array of `{ id: string; name: string; items: ShoppingListItem[] }`
  - Item count derived as `items.length`.

### Create shopping list
- Method: `POST /api/shopping-lists`
- Definition: `Backend/src/Recipes.Api/Endpoints/ShoppingListsEndpoints.cs:22-26`
- Request: `{ name: string }` (required, max 200 chars)
- Response `201 Created`: `{ id: string; name: string }`

### Get shopping list detail
- Method: `GET /api/shopping-lists/{id}`
- Definition: `Backend/src/Recipes.Api/Endpoints/ShoppingListsEndpoints.cs:82-89`
- Response `200 OK`:
  ```
  {
    id: string
    name: string
    items: Array<{
      id: string
      productId: string
      productName: string
      quantity: number
      unit: string
      isPurchased: boolean
      notes: string | null
      sourceType: number     // 1=Manual 2=Recipe 3=Meal plan
      sourceReferenceId: string | null
    }>
  }
  ```
- Response `404 Not Found`: `ProblemDetails`

### Mark item pending (un-purchase)
- Method: `POST /api/shopping-lists/{id}/items/{itemId}/pending`
- Definition: `Backend/src/Recipes.Api/Endpoints/ShoppingListsEndpoints.cs:69-80`
- No request body. Response `204 No Content`.

### Purchase item with expense
- Method: `POST /api/shopping-lists/{id}/items/{itemId}/purchase-with-expense`
- Definition: `Backend/src/Recipes.Api/Endpoints/ShoppingListsEndpoints.cs:104-122`
- Request: `{ amount: number; currency: string; expenseDate: string; description?: string }`
  - amount > 0; currency max 10 chars; expenseDate ISO date; description optional max 500
- Response `204 No Content`.
- Errors: `ProblemDetails`.

### Generate shopping list from meal plan
- Method: `POST /api/meal-plans/{mealPlanId}/shopping-lists/{shoppingListId}`
- Definition: `Backend/src/Recipes.Api/Endpoints/MealPlansEndpoints.cs:54-65`
- No request body. Response `204 No Content`.

### Regenerate shopping list from meal plan
- Method: `POST /api/meal-plans/{mealPlanId}/shopping-lists/{shoppingListId}/regenerate`
- Definition: `Backend/src/Recipes.Api/Endpoints/MealPlansEndpoints.cs:130-141`
- No request body. Response `204 No Content`.

### Supporting endpoints
- `GET /api/meal-plans` — used to populate meal plan selects on the detail page.

## User-visible behavior

- The list page (`/shopping-lists`) shows all shopping lists with name and item count. Empty state: "No shopping lists yet." Loading and error states required.
- The list page has an inline **Create shopping list** form with a single **Name** field. Blank name shows inline error and issues no request. On `201`, the list refreshes and the form resets.
- The list page links each shopping list name to `/shopping-lists/{id}`.
- The detail page (`/shopping-lists/:id`) shows the list name and all items. Each item row shows:
  - Product name (struck-through if purchased).
  - Quantity and unit.
  - Source type label (Manual / Recipe / Meal plan).
  - Notes if present.
  - A **Purchase** button if the item is not yet purchased.
  - A **Mark pending** button if the item is already purchased.
- Clicking **Mark pending** calls the pending endpoint; the list reloads on `204`.
- Clicking **Purchase** opens an inline purchase panel (not a separate route) pre-filling description with the product name. Fields: **Amount** (number, > 0), **Currency** (text, default "BGN"), **Expense date** (date, default today), **Description** (optional). On `204`, panel closes, list reloads.
- The detail page has a **Generate from meal plan** section: a select populated from `GET /api/meal-plans` and a **Generate** button. Select disabled while meal plans are loading. On `204`, the shopping list reloads.
- The detail page has a **Regenerate from meal plan** section: another meal plan select and a **Regenerate** button. On `204`, the shopping list reloads.
- `404` on the detail page shows a not-found state.

## Source type labels (hard-coded)

1=Manual, 2=Recipe, 3=Meal plan

## Acceptance checklist

- [ ] `/shopping-lists` shows list with name and item count, or empty state.
- [ ] Blank name shows inline error and issues no request.
- [ ] On `201`, list refreshes and form resets.
- [ ] `/shopping-lists/:id` shows all items with product name, quantity, unit, source label, notes.
- [ ] Purchased items are struck through; show "Mark pending" button.
- [ ] "Mark pending" calls pending endpoint and reloads on `204`.
- [ ] "Purchase" opens purchase panel pre-filled with product name.
- [ ] Purchase `204` closes panel and reloads list.
- [ ] Generate from meal plan select is populated; `204` reloads list.
- [ ] Regenerate from meal plan select is populated; `204` reloads list.
- [ ] `404` shows not-found state.

## Out of scope

- Adding items manually to a shopping list.
- Removing items from a shopping list.

## Parity reference

React implementation: `Frontend/src/pages/shoppingLists/ShoppingListsPage.tsx`, `Frontend/src/pages/shoppingLists/ShoppingListDetailsPage.tsx`
