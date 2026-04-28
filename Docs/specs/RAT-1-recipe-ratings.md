# RAT-1 — Recipe Ratings

## Summary

Allow authenticated household members to rate recipes on a 1–5 star scale, with an optional short comment. Ratings are scoped to the recipe's household: only members of that household can rate, update, or delete ratings on a recipe. Each user may have at most one rating per recipe (upsert on repeat submission). Aggregate statistics (average stars, count) are surfaced on recipe list cards and recipe detail pages on both frontends.

No AI integration in this slice. Scope is deliberately small: the domain change, three slim API endpoints, and UI on both frontends.

---

## Goals

- Household members can give a 1–5 star rating, optionally with a short comment.
- Repeat submission by the same user overwrites the previous rating (upsert).
- Users can delete their own rating.
- Recipe list shows average ★ and count. Recipe detail shows individual ratings and a form to submit or edit the current user's rating.
- Both React and Angular frontends updated.

## Non-goals

- Public/anonymous ratings. Auth is required (AUTH-1 landed already).
- Cross-household recipe discovery based on ratings.
- Moderation or reporting of rating comments.
- Notifications on new ratings.
- Sorting / filtering recipes by rating. (Read model is enriched — sorting can be a future query enhancement.)

---

## Domain model

### New entity: `RecipeRating`

Owned by the `Recipe` aggregate (same pattern as `RecipeIngredient` and `RecipeStep`).

```
RecipeRating
  RecipeRatingId     readonly record struct (Guid) — new strongly-typed ID
  RecipeId           RecipeId
  UserId             UserId
  Stars              int  (1–5, validated at aggregate method)
  Comment            string?  (max 500 chars; null means no comment)
  CreatedAt          DateTimeOffset
  UpdatedAt          DateTimeOffset?
```

Unique constraint in the DB: `(RecipeId, UserId)`.

### Recipe aggregate changes

Add `_ratings` backing collection and expose read-only collection:

```csharp
IReadOnlyCollection<RecipeRating> Ratings
```

Add two aggregate methods:

```csharp
// Upsert — creates or replaces the caller's rating.
public RecipeRating Rate(UserId userId, int stars, string? comment)

// Returns false (no-op) if the user has no rating to delete.
public bool RemoveRating(UserId userId)
```

`Rate(...)` enforces the 1–5 guard at the domain level (`throw ArgumentOutOfRangeException` if out of range). No domain event needed for this slice — ratings are simple enough that a log entry is sufficient.

### Aggregate statistics

Computed on the fly from the `Ratings` collection; not stored as a denormalised field.

```csharp
public double? AverageStars =>
    _ratings.Count > 0 ? _ratings.Average(r => r.Stars) : null;

public int RatingCount => _ratings.Count;
```

### New strongly-typed ID

```
Recipes.Domain/Primitives/RecipeRatingId.cs
public readonly record struct RecipeRatingId
{
    public Guid Value { get; }
    private RecipeRatingId(Guid value) => Value = value;
    public static RecipeRatingId New() => new(Guid.NewGuid());
    public static RecipeRatingId From(Guid value) => value == Guid.Empty
        ? throw new ArgumentException("RecipeRatingId cannot be empty.")
        : new(value);
}
```

---

## Persistence

### EF Core configuration

New `RecipeRatingConfiguration` in `Persistence/Configurations/`:

- `ToTable("RecipeRatings")`
- `HasKey(r => r.Id)` with value conversion for `RecipeRatingId`
- `HasOne<Recipe>()` with `HasForeignKey("RecipeId")` cascade delete (delete recipe → delete ratings)
- `HasIndex("RecipeId", "UserId").IsUnique()` — enforces one-rating-per-user-per-recipe at DB level
- `Property(r => r.Stars).IsRequired()`
- `Property(r => r.Comment).HasMaxLength(500)`

### EF Core migration

`AddRecipeRatings` — one new table.

### `IRecipeRepository` addition

The existing `GetRecipe` and `ListRecipes` queries need to include ratings so the read model can compute aggregate stats. No new repository method is required: `Include(r => r.Ratings)` added to the existing `GetByIdAsync` and `GetByHouseholdIdsAsync` queries.

---

## API

### New endpoints

All require `[Authorize]` (JWT Bearer). Household membership enforced in handlers using `ICurrentUser.GetHouseholdIdsAsync`.

```
POST /api/recipes/{id}/ratings
  Body: { stars: int, comment?: string }
→ 200 OK   { id, userId, stars, comment, createdAt, updatedAt }
→ 400 Bad Request  (validation failure)
→ 403 Forbidden    (not a household member)
→ 404 Not Found    (recipe not found)

DELETE /api/recipes/{id}/ratings
  (no body — deletes the current user's rating)
→ 204 No Content
→ 403 Forbidden
→ 404 Not Found    (recipe or rating not found)

GET /api/recipes/{id}
  (existing endpoint — enriched to include ratings in response)
→ RecipeDto gains:
    averageStars: number | null
    ratingCount:  number
    ratings: [{ id, userId, stars, comment, createdAt, updatedAt }]
    myRating: { id, stars, comment } | null   ← current user's own rating
```

The `GET /api/recipes` list endpoint adds `averageStars: number | null` and `ratingCount: number` to each item in the list response (no individual ratings in the list — avoid N+1).

### Command / query slices (Application layer)

| Slice | Type | File |
|---|---|---|
| `RateRecipe` | Command | `Application/Recipes/RateRecipe/` |
| `DeleteRecipeRating` | Command | `Application/Recipes/DeleteRecipeRating/` |

`GetRecipe` and `ListRecipes` queries are enhanced (no new slice, just enriched DTOs).

### DTOs

```csharp
public sealed record RecipeRatingDto(
    Guid Id, Guid UserId, int Stars, string? Comment,
    DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);

// Added to RecipeDetailsDto
public double? AverageStars { get; init; }
public int RatingCount { get; init; }
public IReadOnlyList<RecipeRatingDto> Ratings { get; init; }
public RecipeRatingDto? MyRating { get; init; }

// Added to RecipeListItemDto
public double? AverageStars { get; init; }
public int RatingCount { get; init; }
```

---

## Frontend changes

### New translation keys (both `bg.json` and `en.json`)

```json
"ratings": {
  "title": "Ratings",
  "rateThis": "Rate this recipe",
  "yourRating": "Your rating",
  "noRatings": "No ratings yet.",
  "averageLabel": "{{avg}} out of 5 ({{count}} rating)",
  "averageLabelPlural": "{{avg}} out of 5 ({{count}} ratings)",
  "stars": "{{n}} star",
  "starsPlural": "{{n}} stars",
  "comment": "Comment (optional)",
  "commentPlaceholder": "What did you think?",
  "saveRating": "Save rating",
  "deleteRating": "Delete rating",
  "confirmDeleteRating": "Delete your rating for this recipe?",
  "saving": "Saving…",
  "deleting": "Deleting…"
}
```

### React changes

- New `StarRating` presentational component in `src/components/ui/StarRating.tsx` — renders 1–5 clickable stars.
- `RecipeListCard` enriched with average stars + count (read-only display).
- Recipe detail page gains a `RatingSection` component: shows all ratings and a form (`StarRating` + comment textarea + save/delete buttons) pre-populated with the current user's existing rating.
- Two new hooks: `useRateRecipe` (mutation), `useDeleteRecipeRating` (mutation).
- `getRecipe` and `listRecipes` API client functions updated to reflect enriched DTOs.

### Angular changes

- New `StarRatingComponent` (`shared/ui/star-rating/`) — standalone, OnPush, `input()` for value, `output()` for selection.
- `recipes-list` cards enriched with average + count.
- `recipes-details` gains a rating section: list of existing ratings, form to submit / update / delete own rating.
- Two new signals / mutations in `recipes-details.ts`: `rateRecipe()`, `deleteRating()`.
- `RecipesApiClient` updated with `rateRecipe(id, stars, comment?)` and `deleteRating(id)` methods.

---

## Seeder update

Demo recipe records in `DemoDataSeeder` gain a handful of synthetic ratings (2–4 per recipe) from the demo user plus fake user IDs. This exercises the aggregate stats UI on first load without needing real interactions.

---

## Out-of-scope for this slice

- `GET /api/recipes/{id}/ratings` list endpoint (not needed: ratings embedded in recipe detail response).
- Pagination of ratings. Recipe cards in this app are unlikely to accumulate thousands of ratings.
- Rating sort order. Returned newest-first by default.
- Display names on ratings. `UserId` is returned; frontend resolves display names via `/auth/me` comparison (own rating) only. Other raters show as "Anonymous" or initials from UserId for MVP.
