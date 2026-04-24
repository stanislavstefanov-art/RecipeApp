# Spec: person details

Framework-agnostic specification for the person detail page.
Both `/Frontend` (React) and `/FrontendAngular` (Angular) must satisfy this spec.

## Endpoint

### Get person
- Method: `GET /api/persons/{id}`
- Definition: `Backend/src/Recipes.Api/Endpoints/PersonsEndpoints.cs:24-28`
- Response `200 OK`: `{ id: string; name: string; dietaryPreferences: number[]; healthConcerns: number[]; notes?: string }`
- Response `404 Not Found`: `ProblemDetails`

## User-visible behavior

- The detail page (`/persons/:id`) shows:
  - Person name as the page heading.
  - Dietary preferences as a list of human-readable labels using the same mapping as the list page (1=Vegetarian, 2=Pescatarian, 3=Vegan, 4=High protein). Shows "None" when the array is empty.
  - Health concerns as a list of human-readable labels (1=Diabetes, 2=High blood pressure, 3=Gluten intolerance). Shows "None" when the array is empty.
  - Notes text, or "No notes" if absent.
  - Loading, error, and 404 states required.
  - Back link to `/persons`.
- The persons list page has a link from each person's name to `/persons/{id}`.

## Acceptance checklist

- [ ] `/persons/:id` shows name, dietary preference labels, health concern labels, and notes.
- [ ] Empty dietary preferences shows "None".
- [ ] Empty health concerns shows "None".
- [ ] Absent notes shows "No notes".
- [ ] `404` shows not-found state.
- [ ] Person names on the list page link to the detail page.

## Out of scope

- Editing a person.
- Deleting a person.

## Parity reference

React implementation: `Frontend/src/pages/persons/PersonDetailsPage.tsx`
