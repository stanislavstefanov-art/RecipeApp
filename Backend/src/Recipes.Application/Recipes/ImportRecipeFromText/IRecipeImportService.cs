namespace Recipes.Application.Recipes.ImportRecipeFromText;

public interface IRecipeImportService
{
    Task<RecipeExtractionResult> ImportAsync(string text, CancellationToken cancellationToken);
}