using Recipes.Application.Recipes.ImportRecipeFromText;

namespace Recipes.Infrastructure.AI.Claude.Services.Stubs;

public sealed class StubRecipeImportService : IRecipeImportService
{
    public Task<RecipeExtractionResult> ImportAsync(string text, CancellationToken cancellationToken)
    {
        var isRetry = text.Contains("Retry the extraction", StringComparison.OrdinalIgnoreCase);

        var result = isRetry
            ? new RecipeExtractionResult(
                Title: "Imported recipe",
                Servings: 2,
                Ingredients:
                [
                    new RecipeExtractionIngredient("Eggs", 2, "pcs", null),
                    new RecipeExtractionIngredient("Tomato", 1, "pcs", null)
                ],
                Steps:
                [
                    "Prepare the ingredients.",
                    "Cook according to the provided text."
                ],
                Notes: "Stub retry result.",
                Confidence: 0.75,
                NeedsReview: false)
            : new RecipeExtractionResult(
                Title: null,
                Servings: null,
                Ingredients:
                [
                    new RecipeExtractionIngredient("Eggs", 2, "pcs", null)
                ],
                Steps: [],
                Notes: "Stub first-pass result with missing fields.",
                Confidence: 0.45,
                NeedsReview: true);

        return Task.FromResult(result);
    }
}