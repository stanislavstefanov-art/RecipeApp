namespace Recipes.Domain.Entities;

using Recipes.Domain.Primitives;

public sealed class RecipeRating
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
