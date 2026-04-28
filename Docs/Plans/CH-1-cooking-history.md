# CH-1 — Implementation Plan

Reference spec: `Docs/specs/CH-1-cooking-history.md`

Bundles in dependency order: domain → application layer → API → seeder → React → Angular → translations.
Each bundle is a single commit.

---

## Bundle CH-1-1 — Domain, EF Core, repository (≈ 30 minutes)

**Goal:** `CookingLogEntry` entity, `CookingLogEntryId`, `ICookingLogRepository`, EF Core config, migration.

Steps:

1. `Recipes.Domain/Primitives/CookingLogEntryId.cs` — `readonly record struct` with `New()` / `From(Guid)`.

2. `Recipes.Domain/Entities/CookingLogEntry.cs`:
   ```csharp
   public sealed class CookingLogEntry : Entity
   {
       CookingLogEntryId Id     = CookingLogEntryId.New();
       RecipeId          RecipeId
       UserId            UserId
       HouseholdId?      HouseholdId
       DateOnly          CookedOn
       int               Servings    // 1–100
       string?           Notes       // max 500
       DateTimeOffset    CreatedAt

       private CookingLogEntry() { }

       public CookingLogEntry(RecipeId recipeId, UserId userId, HouseholdId? householdId,
           DateOnly cookedOn, int servings, string? notes, DateTimeOffset now)
       { ... }
   }
   ```
   No mutating methods — the entry is immutable after creation.

3. `Recipes.Domain/Repositories/ICookingLogRepository.cs`:
   ```csharp
   void Add(CookingLogEntry entry);
   void Remove(CookingLogEntry entry);
   Task<CookingLogEntry?> GetByIdAsync(CookingLogEntryId id, CancellationToken ct = default);
   Task<IReadOnlyList<CookingLogEntry>> GetByRecipeAndUserAsync(
       RecipeId recipeId, UserId userId, int limit, CancellationToken ct = default);
   Task SaveChangesAsync(CancellationToken ct = default);
   ```

4. `Recipes.Infrastructure/Persistence/Configurations/CookingLogEntryConfiguration.cs`:
   - `ToTable("CookingLogEntries")`
   - Value conversions for `CookingLogEntryId`, `RecipeId`, `UserId`, `HouseholdId`
   - `HasIndex(e => new { e.RecipeId, e.UserId })` — covering index for history queries
   - `Property(e => e.Notes).HasMaxLength(500)`
   - `HasOne<Recipe>().WithMany().HasForeignKey(e => e.RecipeId).OnDelete(DeleteBehavior.Cascade)`

5. `Recipes.Infrastructure/Persistence/RecipesDbContext.cs` — add `DbSet<CookingLogEntry> CookingLogEntries`.

6. `Recipes.Infrastructure/Persistence/CookingLogRepository.cs` — implement interface using `_db`.

7. Register `ICookingLogRepository` → `CookingLogRepository` in `ServiceCollectionExtensions` (scoped).

8. `dotnet ef migrations add AddCookingLog` from repo root.

**Verification:** `dotnet build` clean; migration file generated.

---

## Bundle CH-1-2 — Application layer (≈ 45 minutes)

**Goal:** three vertical slices: `LogCookingEntry`, `DeleteCookingEntry`, `GetRecipeCookingHistory`.

Steps:

1. `CookingLogEntryDto.cs` (new, in `Application/CookingLog/`):
   ```csharp
   public sealed record CookingLogEntryDto(
       Guid Id, Guid RecipeId, string RecipeName,
       DateOnly CookedOn, int Servings, string? Notes, DateTimeOffset CreatedAt);
   ```

2. `LogCookingEntry/LogCookingEntryCommand.cs`:
   ```csharp
   record LogCookingEntryCommand(Guid RecipeId, DateOnly CookedOn, int Servings, string? Notes)
       : IRequest<ErrorOr<CookingLogEntryDto>>;
   ```

3. `LogCookingEntry/LogCookingEntryCommandValidator.cs`:
   - `RecipeId` not empty
   - `CookedOn` `LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))` — future dates rejected
   - `Servings` `InclusiveBetween(1, 100)`
   - `Notes` `MaximumLength(500)` when not null

4. `LogCookingEntry/LogCookingEntryCommandHandler.cs`:
   - Load recipe; 404 if not found
   - Check household membership if recipe has `HouseholdId`
   - Create `CookingLogEntry` (pass `recipe.HouseholdId` for denormalisation)
   - `_cookingLogRepo.Add(entry)`
   - `await _cookingLogRepo.SaveChangesAsync(ct)`
   - Return dto

5. `DeleteCookingEntry/DeleteCookingEntryCommand.cs`:
   ```csharp
   record DeleteCookingEntryCommand(Guid Id) : IRequest<ErrorOr<Deleted>>;
   ```

6. `DeleteCookingEntry/DeleteCookingEntryCommandHandler.cs`:
   - Load entry by id; 404 if not found
   - Verify `entry.UserId == currentUser.UserId`; 404 if mismatch (hides existence)
   - `_cookingLogRepo.Remove(entry)`; save
   - Return `Result.Deleted`

7. `GetRecipeCookingHistory/GetRecipeCookingHistoryQuery.cs`:
   ```csharp
   record GetRecipeCookingHistoryQuery(Guid RecipeId) : IRequest<ErrorOr<IReadOnlyList<CookingLogEntryDto>>>;
   ```

8. `GetRecipeCookingHistory/GetRecipeCookingHistoryHandler.cs`:
   - Load recipe; 404 if not found
   - Check membership
   - `GetByRecipeAndUserAsync(recipeId, currentUser.UserId, limit: 20, ct)`
   - Map to dtos (populate `RecipeName` from loaded recipe)

**Verification:** `dotnet build` clean; `dotnet test` (application unit tests) pass.

---

## Bundle CH-1-3 — API endpoints (≈ 15 minutes)

**Goal:** three endpoints wired in a new `CookingLogEndpoints` class.

Steps:

1. `Recipes.Api/Endpoints/CookingLogEndpoints.cs` (new):
   ```csharp
   var group = app.MapGroup("/api/cooking-log")
       .WithTags("CookingLog")
       .RequireAuthorization();

   group.MapPost("/",
       async (LogCookingEntryRequest req, ISender sender, CancellationToken ct) => ...);
       // → 201 Created at /api/cooking-log/{id}

   group.MapDelete("/{id:guid}",
       async (Guid id, ISender sender, CancellationToken ct) => ...);
       // → 204 No Content

   group.MapGet("/recipe/{recipeId:guid}",
       async (Guid recipeId, ISender sender, CancellationToken ct) => ...);
       // → 200 OK

   record LogCookingEntryRequest(Guid RecipeId, DateOnly CookedOn, int Servings, string? Notes);
   ```

2. Register in `WebApplication` (wherever `MapRecipesEndpoints` is called).

**Verification:** `dotnet run`, call `POST /api/cooking-log` → 201, `GET /api/cooking-log/recipe/{id}` → list.

---

## Bundle CH-1-4 — Seeder additions (≈ 15 minutes)

**Goal:** pre-populate cooking history for the demo user against seeded recipes.

Steps:

Edit `DemoDataSeeder.cs`:
- Use the demo user's `UserId` (already created in the seeder).
- Add 6–8 `CookingLogEntry` records spread over the last 60 days.
- Cover at least three different seeded recipes.
- Vary servings (1–4) and include notes on a couple of entries.

**Verification:** fresh in-memory run → recipe details page shows pre-populated history.

---

## Bundle CH-1-5 — React frontend (≈ 90 minutes)

**Goal:** `CookingHistorySection` on recipe details page, wired to the new API endpoints.

Steps:

1. Add `cookingLogEntrySchema` and `CookingLogEntry` type to `features/recipes/schemas.ts`.

2. Add `logCooking`, `deleteCookingEntry`, `getCookingHistory` to `api/recipes.ts`.

3. `features/recipes/hooks/useCookingHistory.ts` (new):
   ```typescript
   useQuery({ queryKey: ['cookingHistory', recipeId], queryFn: () => getCookingHistory(recipeId) })
   ```

4. `features/recipes/hooks/useLogCooking.ts` (new):
   ```typescript
   useMutation({ mutationFn: ..., onSuccess: () => queryClient.invalidateQueries(['cookingHistory', recipeId]) })
   ```

5. `features/recipes/hooks/useDeleteCookingEntry.ts` (new):
   - Same invalidation pattern.

6. `features/recipes/components/CookingHistorySection.tsx` (new):
   - Renders a log form (date, servings, notes) at the top.
   - Lists past entries (`useCookingHistory`); each row: date, servings label, notes, delete button.
   - Empty state message.
   - Uses `useTranslation()` for all strings.

7. `pages/recipes/RecipeDetailsPage.tsx` — add `<CookingHistorySection recipeId={data.id} />` below `<RatingSection>`.

**Verification:** `npm run build` clean. Log an entry → appears in history. Delete → disappears. Strings in BG/EN.

---

## Bundle CH-1-6 — Angular frontend (≈ 90 minutes)

**Goal:** equivalent feature on the Angular app.

Steps:

1. Add `CookingLogEntryDto` and `LogCookingRequest` to `api/recipes.dto.ts`.

2. Add `logCooking`, `deleteCookingEntry`, `getCookingHistory` to `RecipesClient`.

3. `features/recipes/log-cooking-form.ts/.html` (new standalone component):
   - Typed reactive form: `cookedOn` (`date` input, default today), `servings` (number, default 1), `notes` (optional textarea).
   - `(submitted)` output emits `{ cookedOn: string, servings: number, notes: string | null }`.
   - Validation: `cookedOn` required + no future dates (custom validator), `servings` min 1.

4. `recipes-details.ts` additions:
   - `cookingHistory = rxResource({ params: () => this.id(), stream: ... })`
   - `logCookingState = signal<'idle' | 'logging'>('idle')`
   - `deletingEntryId = signal<string | null>(null)`
   - `onLogCooking(value)` — calls `client.logCooking(...)`, reloads on success
   - `onDeleteCookingEntry(id)` — calls `client.deleteCookingEntry(id)`, reloads on success

5. `recipes-details.html` — add cooking history section after ratings:
   - `<app-log-cooking-form (submitted)="onLogCooking($event)" />`
   - `@for (entry of cookingHistory.value() ?? []; track entry.id)` — row with date, servings, notes, delete button.
   - Loading and empty states.

6. Import `LogCookingFormComponent` in `RecipesDetails`.

**Verification:** `ng build` clean. Same log/delete flow as React.

---

## Bundle CH-1-7 — Translation keys (≈ 15 minutes)

**Goal:** `cookingLog.*` keys in all four locale files.

Add to `Frontend/src/locales/bg.json`, `en.json` and `FrontendAngular/public/i18n/bg.json`, `en.json`:

```json
"cookingLog": {
  "title":           "Cooking history"          / "История на готвене",
  "logCooking":      "Log cooking"              / "Запиши готвене",
  "logging":         "Logging…"                 / "Записване…",
  "cookedOn":        "Date cooked"              / "Дата на готвене",
  "servings":        "Servings"                 / "Порции",
  "notes":           "Notes"                    / "Бележки",
  "notesPlaceholder":"Optional notes…"          / "Незадължителни бележки…",
  "noHistory":       "No cooking history yet."  / "Няма история на готвене все още.",
  "confirmDelete":   "Delete this cooking log entry?" / "Изтрий тази история?",
  "deleting":        "Deleting…"                / "Изтриване…",
  "deleteEntry":     "Delete entry"             / "Изтрий запис",
  "servingsLabel":   "{{count}} serving(s)"     / "{{count}} порц."
}
```

**Verification:** build both apps; click through all cooking-log strings in BG and EN.

---

## Files to modify (cross-bundle index)

| Path | Bundle |
|---|---|
| `Docs/specs/CH-1-cooking-history.md` (new) | preamble |
| `Docs/Plans/CH-1-cooking-history.md` (new) | preamble |
| `Backend/src/Recipes.Domain/Primitives/CookingLogEntryId.cs` (new) | CH-1-1 |
| `Backend/src/Recipes.Domain/Entities/CookingLogEntry.cs` (new) | CH-1-1 |
| `Backend/src/Recipes.Domain/Repositories/ICookingLogRepository.cs` (new) | CH-1-1 |
| `Backend/src/Recipes.Infrastructure/Persistence/Configurations/CookingLogEntryConfiguration.cs` (new) | CH-1-1 |
| `Backend/src/Recipes.Infrastructure/Persistence/RecipesDbContext.cs` | CH-1-1 |
| `Backend/src/Recipes.Infrastructure/Persistence/CookingLogRepository.cs` (new) | CH-1-1 |
| `Backend/src/Recipes.Infrastructure/ServiceCollectionExtensions.cs` | CH-1-1 |
| Migration `AddCookingLog` (new) | CH-1-1 |
| `Backend/src/Recipes.Application/CookingLog/CookingLogEntryDto.cs` (new) | CH-1-2 |
| `Backend/src/Recipes.Application/CookingLog/LogCookingEntry/` (new dir, 3 files) | CH-1-2 |
| `Backend/src/Recipes.Application/CookingLog/DeleteCookingEntry/` (new dir, 2 files) | CH-1-2 |
| `Backend/src/Recipes.Application/CookingLog/GetRecipeCookingHistory/` (new dir, 2 files) | CH-1-2 |
| `Backend/src/Recipes.Api/Endpoints/CookingLogEndpoints.cs` (new) | CH-1-3 |
| `Backend/src/Recipes.Api/Program.cs` | CH-1-3 |
| `Backend/src/Recipes.Infrastructure/Persistence/DemoDataSeeder.cs` | CH-1-4 |
| `Frontend/src/features/recipes/schemas.ts` | CH-1-5 |
| `Frontend/src/api/recipes.ts` | CH-1-5 |
| `Frontend/src/features/recipes/hooks/useCookingHistory.ts` (new) | CH-1-5 |
| `Frontend/src/features/recipes/hooks/useLogCooking.ts` (new) | CH-1-5 |
| `Frontend/src/features/recipes/hooks/useDeleteCookingEntry.ts` (new) | CH-1-5 |
| `Frontend/src/features/recipes/components/CookingHistorySection.tsx` (new) | CH-1-5 |
| `Frontend/src/pages/recipes/RecipeDetailsPage.tsx` | CH-1-5 |
| `FrontendAngular/src/app/api/recipes.dto.ts` | CH-1-6 |
| `FrontendAngular/src/app/api/recipes.client.ts` | CH-1-6 |
| `FrontendAngular/src/app/features/recipes/log-cooking-form.ts` (new) | CH-1-6 |
| `FrontendAngular/src/app/features/recipes/log-cooking-form.html` (new) | CH-1-6 |
| `FrontendAngular/src/app/features/recipes/recipes-details.ts` | CH-1-6 |
| `FrontendAngular/src/app/features/recipes/recipes-details.html` | CH-1-6 |
| `Frontend/src/locales/{bg,en}.json` | CH-1-7 |
| `FrontendAngular/public/i18n/{bg,en}.json` | CH-1-7 |

---

## Verification (end-to-end)

```bash
dotnet run --project Backend/src/Recipes.Api   # http://localhost:5106
cd Frontend && npm run dev                     # http://localhost:5173
cd FrontendAngular && npm run start            # http://localhost:4200
```

Manual checklist:
1. Open a seeded recipe on both apps — cooking history section shows seeded entries.
2. Submit the Log Cooking form (today's date, 2 servings) → entry appears at top of list.
3. Submit with a future date → validation error (client-side and server-side).
4. Delete an entry → it disappears; aggregate refreshes.
5. `POST /api/cooking-log` for another user's recipe → 404 (if household-scoped).
6. `DELETE /api/cooking-log/{othersEntryId}` → 404.
7. Toggle BG/EN — all cooking-log strings translate.
