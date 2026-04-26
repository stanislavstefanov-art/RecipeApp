namespace Recipes.Infrastructure.Options;

public sealed class RecipeDraftReviewOptions
{
    public const string SectionName = "RecipeDraftReview";

    public string Provider { get; init; } = "Stub"; // "Stub" or "Claude"
}
