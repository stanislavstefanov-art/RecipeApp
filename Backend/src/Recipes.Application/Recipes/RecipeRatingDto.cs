namespace Recipes.Application.Recipes;

public sealed record RecipeRatingDto(
    Guid Id,
    Guid UserId,
    int Stars,
    string? Comment,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
