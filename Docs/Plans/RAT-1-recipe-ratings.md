# RAT-1 — Implementation Plan

Reference spec: `Docs/specs/RAT-1-recipe-ratings.md`

Bundles in dependency order: domain → application layer → API → seeder → React → Angular → translations. Each bundle is a single commit.

---

## Bundle RAT-1-1 — Domain & EF Core (≈ 45 minutes)

**Goal:** `RecipeRating` entity owned by `Recipe`, `RecipeRatingId` strongly-typed ID, aggregate methods `Rate` / `RemoveRating`, EF Core config, migration.

Steps:

1. `Recipes.Domain/Primitives/RecipeRatingId.cs` — `readonly record struct` with `New()` and `From(Guid)` guarding empty, matching all other ID types in the project.

2. `Recipes.Domain/Entities/RecipeRating.cs`:
   ```csharp
   public sealed class RecipeRating : Entity
   {
       public RecipeRatingId Id { get; private set; } = RecipeRatingId.New();
       public RecipeId RecipeId { get; private set; }
       public UserId UserId { get; private set; }
       public int Stars { get; private set; }
       public string? Comment { get; private set; }
       public DateTimeOffset CreatedAt { get; private set; }
       public DateTimeOffset? UpdatedAt { get; private set; }

       private RecipeRating() { }

       internal RecipeRating(RecipeId recipeId, UserId userId, int stars, string? comment, DateTimeOffset now)
       {
           RecipeId = recipeId;
           UserId = userId;
           Stars = stars;
           Comment = comment?.Trim();
           CreatedAt = now;
       }

       internal void Update(int stars, string? comment, DateTimeOffset now)
       {
           Stars = stars;
           Comment = comment?.Trim();
           UpdatedAt = now;
       }
   }
   ```

3. `Recipes.Domain/Entities/Recipe.cs` — add backing list and aggregate methods:
   ```csharp
   private readonly List<RecipeRating> _ratings = new();
   public IReadOnlyCollection<RecipeRating> Ratings => _ratings.AsReadOnly();

   public double? AverageStars =>
       _ratings.Count > 0 ? Math.Round(_ratings.Average(r => r.Stars), 1) : null;
   public int RatingCount => _ratings.Count;

   public RecipeRating Rate(UserId userId, int stars, string? comment, DateTimeOffset now)
   {
       if (stars < 1 || stars > 5)
           throw new ArgumentOutOfRangeException(nameof(stars), "Stars must be 1–5.");
       var existing = _ratings.SingleOrDefault(r => r.UserId == userId);
       if (existing is not null) { existing.Update(stars, comment, now); return existing; }
       var rating = new RecipeRating(Id, userId, stars, comment, now);
       _ratings.Add(rating);
       return rating;
   }

   public bool RemoveRating(UserId userId)
   {
       var existing = _ratings.SingleOrDefault(r => r.UserId == userId);
       if (existing is null) return false;
       _ratings.Remove(existing);
       return true;
   }
   ```

4. `Recipes.Infrastructure/Persistence/Configurations/RecipeRatingConfiguration.cs`:
   - `ToTable("RecipeRatings")`
   - `HasKey(r => r.Id)` with `RecipeRatingId` value conversion
   - `HasOne<Recipe>()` with cascade delete (recipe gone → ratings gone)
   - `HasIndex("RecipeId", "UserId").IsUnique()`
   - `Property(r => r.Comment).HasMaxLength(500)`
   - Value conversion for `UserId`

5. `RecipeConfiguration.cs` — add `HasMany(r => r.Ratings).WithOne().HasForeignKey("RecipeId").OnDelete(DeleteBehavior.Cascade)`.

6. `IRecipesDbContext.cs` + `RecipesDbContext.cs` — add `DbSet<RecipeRating> RecipeRatings`.

7. `dotnet ef migrations add AddRecipeRatings --project Backend/src/Recipes.Infrastructure --startup-project Backend/src/Recipes.Api`

**Verification:** `dotnet build Backend/Recipes.sln` passes. Migration file exists with `RecipeRatings` table definition and unique index.

---

## Bundle RAT-1-2 — Application layer (≈ 60 minutes)

**Goal:** `RateRecipe` command, `DeleteRecipeRating` command, enriched `GetRecipe` and `ListRecipes` DTOs.

Steps:

1. `Recipes.Application/Recipes/RateRecipe/`:
   - `RateRecipeCommand.cs` — `record (RecipeId RecipeId, int Stars, string? Comment) : IRequest<ErrorOr<RecipeRatingDto>>`.
   - `RateRecipeCommandValidator.cs` — `Stars` in range 1–5, `Comment` max 500 chars.
   - `RateRecipeCommandHandler.cs`:
     ```
     - Get recipe by id (404 if not found)
     - Verify caller is member of recipe.HouseholdId (403 if not)
     - Call recipe.Rate(currentUser.UserId, stars, comment, DateTimeOffset.UtcNow)
     - Save
     - Return RecipeRatingDto mapped from the returned RecipeRating
     ```

2. `Recipes.Application/Recipes/DeleteRecipeRating/`:
   - `DeleteRecipeRatingCommand.cs` — `record (RecipeId RecipeId) : IRequest<ErrorOr<Deleted>>`.
   - No separate validator needed (RecipeId is a route param, already typed).
   - `DeleteRecipeRatingCommandHandler.cs`:
     ```
     - Get recipe by id (404 if not found)
     - Verify membership (403)
     - Call recipe.RemoveRating(currentUser.UserId)
     - If returned false → 404 (user had no rating)
     - Save
     - Return Result.Deleted
     ```

3. `RecipeRatingDto.cs` (new shared DTO in `Application/Recipes/`):
   ```csharp
   public sealed record RecipeRatingDto(
       Guid Id, Guid UserId, int Stars, string? Comment,
       DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);
   ```

4. `RecipeDetailsDto` (existing in `GetRecipe/`) — add:
   ```csharp
   public double? AverageStars { get; init; }
   public int RatingCount { get; init; }
   public IReadOnlyList<RecipeRatingDto> Ratings { get; init; } = [];
   public RecipeRatingDto? MyRating { get; init; }
   ```
   Update the mapping in `GetRecipeHandler` to populate these fields. `MyRating` is the rating where `rating.UserId == currentUser.UserId`.

5. `RecipeListItemDto` (existing in `ListRecipes/`) — add `AverageStars` and `RatingCount`. Update `ListRecipesHandler` mapping.

6. `IRecipeRepository` — update `GetByIdAsync` and `GetByHouseholdIdsAsync` to `Include(r => r.Ratings)`.

**Verification:** `dotnet test` passes (no new tests required this bundle — existing handler tests still compile).

---

## Bundle RAT-1-3 — API endpoints (≈ 20 minutes)

**Goal:** `POST /api/recipes/{id}/ratings` and `DELETE /api/recipes/{id}/ratings` wired up.

Steps:

1. `RecipesEndpoints.cs` — inside the existing `/api/recipes/{id}` group or alongside it:
   ```
   POST   /api/recipes/{id}/ratings
     → dispatch RateRecipeCommand
     → 200 RecipeRatingDto on success

   DELETE /api/recipes/{id}/ratings
     → dispatch DeleteRecipeRatingCommand
     → 204 No Content on success
   ```
   Both require `[Authorize]`. Map `Error.NotFound` → 404, `Error.Forbidden` → 403.

2. No changes to existing `GET /api/recipes/{id}` or `GET /api/recipes` route registrations — they already dispatch to `GetRecipeQuery` / `ListRecipesQuery`; the enriched DTOs flow automatically.

**Verification:** `dotnet run --project Backend/src/Recipes.Api` with InMemory DB. Curl a `POST /api/recipes/{id}/ratings` with a valid JWT from the seeded demo user — expect `200`. Second POST (same user) should overwrite. `DELETE` should return `204`. `GET /api/recipes/{id}` response now contains `averageStars`, `ratingCount`, `ratings`, `myRating`.

---

## Bundle RAT-1-4 — Seeder update (≈ 15 minutes)

**Goal:** Demo recipes have synthetic ratings so aggregate stats render on first launch.

Steps:

1. `DemoDataSeeder.cs` — after creating demo recipes, call `recipe.Rate(demoUser.Id, stars, comment, DateTimeOffset.UtcNow)` for 2–3 ratings per recipe using the `demoUser` plus two synthetic `UserId`s (static `Guid.Parse("…")` values — stable across restarts). Use a small array of canned comments ("Great weeknight dish", "Needs more garlic", null, etc.) and vary stars 3–5.
2. No migration required — same EF schema.

**Verification:** Start the API. Call `GET /api/recipes` — response items show `averageStars` and `ratingCount` > 0.

---

## Bundle RAT-1-5 — React frontend (≈ 90 minutes)

**Goal:** Star rating display on recipe list cards and full rating UX on recipe detail page.

Steps:

1. Update `src/features/recipes/api/recipesClient.ts` — add `rateRecipe(id, stars, comment?)` and `deleteRecipeRating(id)` typed functions. Update `RecipeDetailsDto` and `RecipeListItemDto` types to include new fields.

2. `src/components/ui/StarRating.tsx` — new presentational component:
   - Props: `value: number | null` (current stars, 0 = unset), `onChange?: (stars: number) => void` (read-only if omitted), `size?: 'sm' | 'md'`.
   - Renders 5 `<button>` elements (or `<span>` if read-only). Filled ★ for `i <= value`, empty ☆ otherwise.
   - Accessible: `aria-label="Rate {{n}} stars"`.

3. Recipe list card — add `<StarRating value={recipe.averageStars} size="sm" />` and `({recipe.ratingCount})` beneath the recipe name.

4. `src/features/recipes/hooks/useRateRecipe.ts` — `useMutation` wrapping `rateRecipe`, invalidates `['recipe', id]` on success.

5. `src/features/recipes/hooks/useDeleteRecipeRating.ts` — `useMutation` wrapping `deleteRecipeRating`, invalidates `['recipe', id]`.

6. `src/features/recipes/components/RatingSection.tsx` — new component:
   - Props: `recipeId`, `ratings: RecipeRatingDto[]`, `myRating: RecipeRatingDto | null`, `averageStars: number | null`, `ratingCount: number`.
   - Shows aggregate summary (e.g. "★ 4.2 — 7 ratings").
   - Form: `<StarRating>` + comment `<textarea>` pre-filled from `myRating`, Save and Delete (if rated) buttons.
   - On save calls `useRateRecipe`; on delete calls `useDeleteRecipeRating` (after `window.confirm`).
   - Lists existing ratings (stars + comment, date) ordered newest first.

7. Recipe detail page — render `<RatingSection>` below the recipe body.

**Verification:** `npm run build` passes. On the recipe detail page, the demo seeded ratings are visible. Clicking stars + Save creates / updates a rating. Delete removes it.

---

## Bundle RAT-1-6 — Angular frontend (≈ 90 minutes)

**Goal:** Mirror RAT-1-5 idiomatically in Angular (signals, translate pipe, OnPush).

Steps:

1. `FrontendAngular/src/app/api/recipes.dto.ts` — add `averageStars`, `ratingCount`, `ratings`, `myRating` to `RecipeDetailsDto`; add `averageStars`, `ratingCount` to `RecipeListItemDto`. Add `RecipeRatingDto` type.

2. `FrontendAngular/src/app/api/recipes.client.ts` — add `rateRecipe(id, stars, comment?)` and `deleteRating(id)` methods.

3. `FrontendAngular/src/app/shared/ui/star-rating/star-rating.ts` and `star-rating.html` — new standalone OnPush component:
   - `value = input<number | null>(null)`, `readonly = input<boolean>(false)`, `rated = output<number>()`.
   - Template: `@for (n of [1,2,3,4,5]; track n) { <button ...> }`.

4. `recipes-list.html` — add `<app-star-rating [value]="recipe.averageStars" [readonly]="true" />` and count to each card.

5. `recipes-details.ts` — add signals `ratingPending = signal(false)`, `selectedStars = signal<number | null>(null)`, `ratingComment = signal('')`. Wire `rateRecipe()` and `deleteRating()` methods using `HttpClient` via the API client (or `rxResource`). Invalidate the detail resource on mutation success by calling `resource.reload()`.

6. `recipes-details.html` — add rating section below the recipe body: aggregate summary, `<app-star-rating>` for current user input, comment textarea, Save and Delete buttons, list of existing ratings.

**Verification:** `npm run build` passes. Same manual check as React — seeded ratings visible, rating submit works.

---

## Bundle RAT-1-7 — Translation keys (≈ 15 minutes)

**Goal:** `ratings.*` key block added to all four locale files (React bg/en, Angular bg/en) with full parity.

Keys to add (see spec for full list):
- `ratings.title`, `ratings.rateThis`, `ratings.yourRating`, `ratings.noRatings`
- `ratings.averageLabel`, `ratings.comment`, `ratings.commentPlaceholder`
- `ratings.saveRating`, `ratings.deleteRating`, `ratings.confirmDeleteRating`
- `ratings.saving`, `ratings.deleting`

**Verification:** `node -e "..."` parity check shows the `ratings` section identical across all four files.

---

## Files to modify (index)

| Path | Bundle |
|---|---|
| `Docs/specs/RAT-1-recipe-ratings.md` (new) | spec |
| `Backend/src/Recipes.Domain/Primitives/RecipeRatingId.cs` (new) | RAT-1-1 |
| `Backend/src/Recipes.Domain/Entities/RecipeRating.cs` (new) | RAT-1-1 |
| `Backend/src/Recipes.Domain/Entities/Recipe.cs` | RAT-1-1 |
| `Backend/src/Recipes.Infrastructure/Persistence/Configurations/RecipeRatingConfiguration.cs` (new) | RAT-1-1 |
| `Backend/src/Recipes.Infrastructure/Persistence/Configurations/RecipeConfiguration.cs` | RAT-1-1 |
| `Backend/src/Recipes.Infrastructure/Persistence/RecipesDbContext.cs` | RAT-1-1 |
| `Backend/src/Recipes.Application/Recipes/RateRecipe/` (new slice, 3 files) | RAT-1-2 |
| `Backend/src/Recipes.Application/Recipes/DeleteRecipeRating/` (new slice, 2 files) | RAT-1-2 |
| `Backend/src/Recipes.Application/Recipes/RecipeRatingDto.cs` (new) | RAT-1-2 |
| `Backend/src/Recipes.Application/Recipes/GetRecipe/RecipeDetailsDto.cs` | RAT-1-2 |
| `Backend/src/Recipes.Application/Recipes/GetRecipe/GetRecipeHandler.cs` | RAT-1-2 |
| `Backend/src/Recipes.Application/Recipes/ListRecipes/RecipeListItemDto.cs` | RAT-1-2 |
| `Backend/src/Recipes.Application/Recipes/ListRecipes/ListRecipesHandler.cs` | RAT-1-2 |
| `Backend/src/Recipes.Domain/Repositories/IRecipeRepository.cs` | RAT-1-2 |
| `Backend/src/Recipes.Api/Endpoints/RecipesEndpoints.cs` | RAT-1-3 |
| `Backend/src/Recipes.Infrastructure/Persistence/DemoDataSeeder.cs` | RAT-1-4 |
| `Frontend/src/features/recipes/api/recipesClient.ts` | RAT-1-5 |
| `Frontend/src/components/ui/StarRating.tsx` (new) | RAT-1-5 |
| `Frontend/src/features/recipes/hooks/useRateRecipe.ts` (new) | RAT-1-5 |
| `Frontend/src/features/recipes/hooks/useDeleteRecipeRating.ts` (new) | RAT-1-5 |
| `Frontend/src/features/recipes/components/RatingSection.tsx` (new) | RAT-1-5 |
| `Frontend/src/pages/recipes/RecipeDetailPage.tsx` | RAT-1-5 |
| `Frontend/src/pages/recipes/RecipesListPage.tsx` (or card component) | RAT-1-5 |
| `FrontendAngular/src/app/api/recipes.dto.ts` | RAT-1-6 |
| `FrontendAngular/src/app/api/recipes.client.ts` | RAT-1-6 |
| `FrontendAngular/src/app/shared/ui/star-rating/` (new, 2 files) | RAT-1-6 |
| `FrontendAngular/src/app/features/recipes/recipes-list.html` | RAT-1-6 |
| `FrontendAngular/src/app/features/recipes/recipes-details.ts` | RAT-1-6 |
| `FrontendAngular/src/app/features/recipes/recipes-details.html` | RAT-1-6 |
| `Frontend/src/locales/{bg,en}.json` | RAT-1-7 |
| `FrontendAngular/public/i18n/{bg,en}.json` | RAT-1-7 |

---

## Recommended execution order

`RAT-1-1 → RAT-1-2 → RAT-1-3 → RAT-1-4 → RAT-1-5 → RAT-1-6 → RAT-1-7`

All bundles are serial — each depends on the previous. Backend must be done before frontend because the enriched DTOs drive the TypeScript types.

---

## Verification (end-to-end)

```bash
# Start backend
dotnet run --project Backend/src/Recipes.Api

# React
cd Frontend && npm run dev    # http://localhost:5173

# Angular
cd FrontendAngular && npm run start  # http://localhost:4200
```

Manual checklist:
1. Open recipe list — each card shows ★ average and count from seeded data.
2. Open a recipe detail — ratings section shows seeded ratings.
3. Click stars + Save — rating appears in the list, aggregate updates.
4. Click Save again with different stars — own rating updates in place.
5. Click Delete — own rating removed, aggregate recalculates.
6. Verify in BG and EN that all strings in the ratings section are translated.
