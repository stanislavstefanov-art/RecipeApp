# CH-1 — Cooking History

## Problem

Users have no way to record when they actually cooked a recipe. Meal plans capture
*intended* meals; cooking history captures *what was actually cooked*. This gap means
there is no answer to "when did I last make this?", "how many times have I cooked this
recipe?", or "what did I cook last week?".

## Goal

Allow authenticated users to log cooking events ("I cooked this recipe today") and see
that history on the recipe detail page. The first version is recipe-centric: logging and
history are surfaced within the recipe detail page, not as a top-level list.

---

## Domain model

### New entity: `CookingLogEntry`

Standalone aggregate root (independent table, independently queryable).

```csharp
public sealed class CookingLogEntry : Entity
{
    CookingLogEntryId Id     // RecordStruct; New() / From(Guid)
    RecipeId          RecipeId
    UserId            UserId
    HouseholdId?      HouseholdId   // denormalised for efficient household-scoped queries
    DateOnly          CookedOn      // day-level precision — cooking is a daily event
    int               Servings      // ≥ 1, default 1
    string?           Notes         // max 500 chars
    DateTimeOffset    CreatedAt
}
```

No aggregate methods beyond creation (it is effectively an immutable log entry). There
is no "update" operation; corrections are delete-then-recreate.

### New strongly-typed ID

`CookingLogEntryId` — same `readonly record struct` pattern as `RecipeRatingId`.

### No changes to `Recipe`

Cooking history is not owned by the `Recipe` aggregate. Loading recipe details must not
pull in unbounded cooking-log rows.

---

## Application layer

### Commands / Queries (vertical slices)

| Slice | Type | Returns |
|---|---|---|
| `LogCookingEntry` | Command | `ErrorOr<CookingLogEntryDto>` |
| `DeleteCookingEntry` | Command | `ErrorOr<Deleted>` |
| `GetRecipeCookingHistory` | Query | `ErrorOr<IReadOnlyList<CookingLogEntryDto>>` |

### `CookingLogEntryDto`

```csharp
record CookingLogEntryDto(
    Guid Id,
    Guid RecipeId,
    string RecipeName,   // denormalised for list display
    DateOnly CookedOn,
    int Servings,
    string? Notes,
    DateTimeOffset CreatedAt
);
```

### `LogCookingEntryCommand`

```csharp
record LogCookingEntryCommand(Guid RecipeId, DateOnly CookedOn, int Servings, string? Notes)
    : IRequest<ErrorOr<CookingLogEntryDto>>;
```

Validator:
- `RecipeId` not empty
- `CookedOn` ≤ today (cannot log future cooking)
- `Servings` InclusiveBetween(1, 100)
- `Notes` MaximumLength(500) when not null

Handler:
1. Load the recipe by id.
2. Check access (household membership if recipe has `HouseholdId`).
3. Create a new `CookingLogEntry`.
4. Save.
5. Return dto.

### `DeleteCookingEntryCommand`

```csharp
record DeleteCookingEntryCommand(Guid Id) : IRequest<ErrorOr<Deleted>>;
```

Handler:
1. Load entry by id.
2. Verify `entry.UserId == currentUser.UserId` (own entries only; returns 404 otherwise
   to avoid leaking existence).
3. Delete.

### `GetRecipeCookingHistoryQuery`

```csharp
record GetRecipeCookingHistoryQuery(Guid RecipeId) : IRequest<ErrorOr<IReadOnlyList<CookingLogEntryDto>>>;
```

Handler:
1. Verify recipe exists and user has access.
2. Return entries where `RecipeId == recipeId AND UserId == currentUser.UserId`,
   ordered `CookedOn DESC`, limited to the **most recent 20** entries.

`RecipeName` in the dto is populated from the loaded `Recipe.Name.Value`.

---

## Repository

New interface `ICookingLogRepository` in `Recipes.Domain`:

```csharp
public interface ICookingLogRepository
{
    void Add(CookingLogEntry entry);
    void Remove(CookingLogEntry entry);
    Task<CookingLogEntry?> GetByIdAsync(CookingLogEntryId id, CancellationToken ct = default);
    Task<IReadOnlyList<CookingLogEntry>> GetByRecipeAndUserAsync(
        RecipeId recipeId, UserId userId, int limit, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

EF Core implementation in `Recipes.Infrastructure`.

---

## EF Core

`CookingLogEntries` table:

| Column | Type | Notes |
|---|---|---|
| `Id` | `uniqueidentifier` | PK, value-converted |
| `RecipeId` | `uniqueidentifier` | FK → Recipes, cascade delete |
| `UserId` | `uniqueidentifier` | value-converted |
| `HouseholdId` | `uniqueidentifier?` | nullable, no FK constraint |
| `CookedOn` | `date` | `DateOnly` mapping |
| `Servings` | `int` | |
| `Notes` | `nvarchar(500)?` | |
| `CreatedAt` | `datetimeoffset` | |

Indexes:
- `(RecipeId, UserId, CookedOn DESC)` — covers `GetByRecipeAndUserAsync`

Cascade delete from `Recipe` (deleting a recipe removes its cooking log entries).

`DateOnly` mapped via EF Core's built-in `DateOnly` support (EF 8+).

---

## API endpoints

All require `RequireAuthorization()`.

```
POST   /api/cooking-log                       LogCookingEntryCommand
DELETE /api/cooking-log/{id:guid}             DeleteCookingEntryCommand
GET    /api/cooking-log/recipe/{recipeId:guid} GetRecipeCookingHistoryQuery
```

HTTP shape:

```
POST /api/cooking-log
Body: { "recipeId": "...", "cookedOn": "2026-04-28", "servings": 4, "notes": "..." }
→ 201 Created, body: CookingLogEntryDto

DELETE /api/cooking-log/{id}
→ 204 No Content

GET /api/cooking-log/recipe/{recipeId}
→ 200 OK, body: CookingLogEntryDto[]
```

`CookedOn` serialised as ISO 8601 date string (`"2026-04-28"`).

---

## Seeder additions

Add 5–8 realistic cooking log entries for the seeded demo user against the seeded
recipes. Spread across the last 60 days. Vary servings (1–6).

---

## React frontend

### Location

The cooking history section appears at the bottom of the Recipe Details page, below the
Ratings section.

### UI components

**`LogCookingForm`** (new, under `features/recipes/components/`):
- Date input (defaults to today)
- Servings number input (default 1, min 1, max 100)
- Optional notes textarea (max 500 chars)
- Submit button: "Log cooking" / "Logging…"

**`CookingHistorySection`** (new, under `features/recipes/components/`):
- Heading: "Cooking history"
- Renders `LogCookingForm` at the top
- Lists up to 20 past entries (date, servings, notes, delete button)
- Empty state: "No cooking history yet."

### Hooks (new, under `features/recipes/hooks/`)

- `useLogCooking(recipeId)` — `useMutation`, on success invalidates `['cookingHistory', recipeId]`
- `useDeleteCookingEntry(recipeId)` — `useMutation`, on success invalidates same key
- `useCookingHistory(recipeId)` — `useQuery(['cookingHistory', recipeId])`

### Schema (added to `features/recipes/schemas.ts`)

```typescript
export const cookingLogEntrySchema = z.object({
  id: z.string().uuid(),
  recipeId: z.string().uuid(),
  recipeName: z.string(),
  cookedOn: z.string(),       // ISO date string
  servings: z.number().int().min(1),
  notes: z.string().nullable().default(null),
  createdAt: z.string(),
});
export type CookingLogEntry = z.infer<typeof cookingLogEntrySchema>;
```

### API functions (added to `api/recipes.ts`)

```typescript
logCooking(recipeId, cookedOn, servings, notes?)
deleteCookingEntry(id)
getCookingHistory(recipeId): CookingLogEntry[]
```

---

## Angular frontend

### Location

Same as React: bottom of recipe details, below ratings section.

### Components

**`LogCookingFormComponent`** (`features/recipes/log-cooking-form.ts/.html`):
- Typed reactive form: `cookedOn` (date), `servings` (number, default 1), `notes` (optional)
- `(submitted)` output emits the form value
- OnPush, standalone

**Rating section in `recipes-details.html`**:
- Add `<app-log-cooking-form>` + history list after the ratings section
- History list: `@for` over `cookingHistory()` signal, each item shows date, servings,
  notes, delete button

**`RecipesDetails` additions**:
- `cookingHistory = rxResource(...)` — calls `getCookingHistory(id())`
- `logCookingState` signal for loading state
- `deleteCookingState` signal map or single signal for delete state
- `onLogCooking(value)` — calls `client.logCooking(...)`, reloads `cookingHistory` on success
- `onDeleteCookingEntry(id)` — calls `client.deleteCookingEntry(id)`, reloads on success

### `RecipesClient` additions

```typescript
logCooking(recipeId, cookedOn, servings, notes?): Observable<CookingLogEntryDto>
deleteCookingEntry(id): Observable<void>
getCookingHistory(recipeId): Observable<CookingLogEntryDto[]>
```

### New DTO types (in `api/recipes.dto.ts`)

```typescript
interface CookingLogEntryDto { id, recipeId, recipeName, cookedOn, servings, notes, createdAt }
interface LogCookingRequest { recipeId, cookedOn, servings, notes? }
```

---

## Translation keys

New namespace `cookingLog` in all four locale files:

```json
{
  "cookingLog": {
    "title": "Cooking history",
    "logCooking": "Log cooking",
    "logging": "Logging…",
    "cookedOn": "Date cooked",
    "servings": "Servings",
    "notes": "Notes",
    "notesPlaceholder": "Optional notes…",
    "noHistory": "No cooking history yet.",
    "delete": "Delete entry",
    "confirmDelete": "Delete this cooking log entry?",
    "deleting": "Deleting…",
    "servingsLabel": "{{count}} serving",
    "servingsLabel_plural": "{{count}} servings"
  }
}
```

Bulgarian equivalents in `bg.json`.

---

## Out of scope

- A standalone `/cooking-log` page listing all cooking history across all recipes (future
  CH-2).
- Household-visible cooking history (other members' entries visible to the household) —
  currently each user sees their own history only.
- Statistics / frequency analysis (future).
- "Cook from shopping list" integration.
- Editing a log entry (delete-then-recreate is sufficient for v1).

---

## Acceptance criteria

1. From either app's recipe details page, submit the Log Cooking form — a new entry
   appears in the history list.
2. The `CookedOn` date defaults to today and cannot be set to a future date.
3. Servings must be ≥ 1. Notes are optional.
4. Clicking Delete on a history entry removes it; the list refreshes.
5. Seeded data shows pre-populated cooking history on at least two seeded recipes.
6. All strings are localised in Bulgarian and English.
7. Backend: `POST /api/cooking-log` with a future `cookedOn` returns 400.
8. Backend: `DELETE /api/cooking-log/{id}` for another user's entry returns 404.
