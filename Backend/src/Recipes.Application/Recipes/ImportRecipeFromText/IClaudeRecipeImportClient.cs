namespace Recipes.Application.Recipes.ImportRecipeFromText;

public interface IClaudeRecipeImportClient
{
    Task<RecipeExtractionResult> ImportAsync(
        string inputText,
        string promptTemplate,
        string jsonSchema,
        CancellationToken cancellationToken);
}