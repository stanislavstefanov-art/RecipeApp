# Spec: households (list, create, detail, add member)

Framework-agnostic specification for household management.
Both `/Frontend` (React) and `/FrontendAngular` (Angular) must satisfy this spec.

## Endpoints

### List households
- Method: `GET /api/households`
- Definition: `Backend/src/Recipes.Api/Endpoints/HouseholdsEndpoints.cs:17-21`
- Response `200 OK`: array of `{ id: string; name: string; memberCount: number }`

### Create household
- Method: `POST /api/households`
- Definition: `Backend/src/Recipes.Api/Endpoints/HouseholdsEndpoints.cs:23-27`
- Request: `{ name: string }` (required, max 200 chars)
- Response `201 Created`: `{ id: string; name: string }`
- Validation: `CreateHouseholdValidator.cs` — name required, max 200.

### Get household details
- Method: `GET /api/households/{id}`
- Definition: `Backend/src/Recipes.Api/Endpoints/HouseholdsEndpoints.cs:29-33`
- Response `200 OK`:
  ```
  {
    id: string
    name: string
    members: Array<{
      personId: string
      personName: string
      dietaryPreferences: number[]
      healthConcerns: number[]
      notes: string | null
    }>
  }
  ```
- Response `404 Not Found`: `ProblemDetails` when household does not exist.

### Add member
- Method: `POST /api/households/{householdId}/members/{personId}`
- Definition: `Backend/src/Recipes.Api/Endpoints/HouseholdsEndpoints.cs:35-43`
- No request body.
- Response `204 No Content` on success.
- Errors: `ProblemDetails` (e.g. person not found, already a member).

## User-visible behavior

- The list page (`/households`) shows all households with their name and member count. Empty state: "No households yet." Loading and error states required.
- The list page has a **New household** link to `/households/new`.
- The create page (`/households/new`) has a single **Name** field (required, max 200 chars). On `201`, navigates to `/households/{id}` (the new household's detail page).
- The detail page (`/households/{id}`) shows the household name and a members list with each member's name, dietary preference badges, and health concern badges (same label mapping as the persons slice).
- The detail page also shows an **Add member** form: a `<select>` populated from `GET /api/persons`, a **Add** button. The select shows person names; the selected person's ID is used in the POST path.
- While persons are loading, the select is disabled.
- On `204`, the household is reloaded and the select resets to its default state.
- On error adding a member, an inline error message is shown near the button.
- The detail page shows a `404` not-found state when the household does not exist.

## Acceptance checklist

- [ ] `/households` shows list with name and member count, or empty state.
- [ ] "New household" navigates to `/households/new`.
- [ ] Blank name shows inline error and issues no request.
- [ ] On `201`, navigates to `/households/{id}`.
- [ ] `/households/{id}` shows name and members list with preference/concern badges.
- [ ] The add-member select is populated from `GET /api/persons`.
- [ ] On `204` the household reloads and the select resets.
- [ ] On add-member error, an inline error is shown.
- [ ] `404` household shows a not-found state.

## Out of scope

- Removing a member from a household.
- Renaming a household.
- Deleting a household.

## Parity notes

**New feature — no React implementation exists yet.** Angular is the reference implementation.

## Parity reference

React implementation: none yet.
