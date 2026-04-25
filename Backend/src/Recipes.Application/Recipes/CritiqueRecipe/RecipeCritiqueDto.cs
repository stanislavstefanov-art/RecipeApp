namespace Recipes.Application.Recipes.CritiqueRecipe;

public sealed record RecipeCritiqueDto(
    IReadOnlyList<CritiqueDimensionDto> Dimensions,
    string Summary,
    string Confidence,
    bool NeedsReview);

public sealed record CritiqueDimensionDto(
    string Dimension,
    int Rating,
    string Rationale,
    IReadOnlyList<string> Suggestions);
