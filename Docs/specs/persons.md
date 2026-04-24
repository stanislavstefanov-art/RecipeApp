# Spec: persons (list and create)

Framework-agnostic specification for listing and creating persons.
Both `/Frontend` (React) and `/FrontendAngular` (Angular) must satisfy this spec.

## Endpoints

### List persons
- Method: `GET /api/persons`
- Definition: `Backend/src/Recipes.Api/Endpoints/PersonsEndpoints.cs:16-20`
- No request body.
- Response `200 OK`: array of
  ```
  {
    id: string
    name: string
    dietaryPreferences: number[]   // DietaryPreference enum values
    healthConcerns: number[]        // HealthConcern enum values
    notes: string | null
  }
  ```

### Create person
- Method: `POST /api/persons`
- Definition: `Backend/src/Recipes.Api/Endpoints/PersonsEndpoints.cs:22-33`
- Request body:
  ```
  {
    name: string                     // required, max 200 chars
    dietaryPreferences: number[]     // DietaryPreference enum values, may be empty
    healthConcerns: number[]         // HealthConcern enum values, may be empty
    notes: string | null             // optional, max 1000 chars
  }
  ```
- Response `201 Created`: `{ id: string; name: string }`
- Validation: `CreatePersonValidator.cs` — name required/max 200, each preference/concern must be a valid enum value, notes max 1000.
- Errors: `ProblemDetails` via `ErrorOrExtensions.ToHttpResult()`.

### Enum values (both frontend and backend must agree)
`Backend/src/Recipes.Domain/Enums/DietaryPreference.cs`:
- 0 = None, 1 = Vegetarian, 2 = Pescatarian, 3 = Vegan, 4 = HighProtein

`Backend/src/Recipes.Domain/Enums/HealthConcern.cs`:
- 0 = None, 1 = Diabetes, 2 = HighBloodPressure, 3 = GlutenIntolerance

## User-visible behavior

- The persons feature lives under the route `/persons`.
- The list page (`/persons`) shows all persons. Each row displays the person's name plus their dietary preferences and health concerns as human-readable labels (not raw numbers).
- The list has a **New person** button/link that navigates to `/persons/new`.
- The create page (`/persons/new`) has:
  - **Name** field (required text input, max 200 chars).
  - **Dietary preferences** — a set of checkboxes, one per non-None preference value (Vegetarian, Pescatarian, Vegan, High protein). Multiple may be selected.
  - **Health concerns** — a set of checkboxes, one per non-None concern value (Diabetes, High blood pressure, Gluten intolerance). Multiple may be selected.
  - **Notes** optional textarea (max 1000 chars).
  - A **Create** button.
- Submitting with a blank name shows a client-side inline error and issues no request.
- On `201 Created`, the browser navigates to `/persons`.
- On `400` or `5xx`, a non-empty error message is displayed and the form stays interactive.
- While the request is in flight the button is disabled.
- The list page shows all four standard states: loading, error, empty ("No persons yet"), and the list.

## Acceptance checklist

- [ ] `/persons` shows a list of persons with name and human-readable preference/concern labels.
- [ ] Empty list shows a helpful empty state.
- [ ] "New person" link navigates to `/persons/new`.
- [ ] Submitting with a blank name shows a client-side inline error and issues no request.
- [ ] On `201`, navigates to `/persons`.
- [ ] On `400` or `5xx`, a visible error message is displayed.
- [ ] Dietary preference and health concern checkboxes display human-readable labels.
- [ ] Selections are correctly serialized as integer arrays in the request body.

## Out of scope

- Editing or deleting a person.
- Person detail page (`GET /api/persons/{id}`) — deferred until needed by households.
- Validating that enum values sent match backend-allowed values (the server returns 400 on unknown values).

## Parity notes

**New feature — no React implementation exists yet.** Angular is the reference implementation.

## Parity reference

React implementation: none yet.
