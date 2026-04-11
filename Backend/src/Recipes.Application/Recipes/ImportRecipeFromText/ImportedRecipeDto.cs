namespace Recipes.Application.Recipes.ImportRecipeFromText;

public sealed record ImportedRecipeDto(
    string? Title,
    int? Servings,
    IReadOnlyList<ImportedIngredientDto> Ingredients,
    IReadOnlyList<string> Steps,
    string? Notes,
    double Confidence,
    bool NeedsReview);

public sealed record ImportedIngredientDto(
    string Name,
    decimal? Quantity,
    string? Unit,
    string? Notes);