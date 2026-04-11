namespace Recipes.Application.Recipes.ImportRecipeFromText;

public sealed record RecipeExtractionResult(
    string? Title,
    int? Servings,
    IReadOnlyList<RecipeExtractionIngredient> Ingredients,
    IReadOnlyList<string> Steps,
    string? Notes,
    double Confidence,
    bool NeedsReview);

public sealed record RecipeExtractionIngredient(
    string Name,
    decimal? Quantity,
    string? Unit,
    string? Notes);