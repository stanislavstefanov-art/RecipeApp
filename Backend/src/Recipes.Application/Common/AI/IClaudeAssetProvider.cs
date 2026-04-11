namespace Recipes.Application.Common.AI;

public interface IClaudeAssetProvider
{
    Task<string> GetRecipeImportPromptAsync(CancellationToken cancellationToken);
    Task<string> GetRecipeImportSchemaAsync(CancellationToken cancellationToken);
    Task<string> GetMealPlanSuggestionPromptAsync(CancellationToken cancellationToken);
    Task<string> GetMealPlanSuggestionSchemaAsync(CancellationToken cancellationToken);
    Task<string> GetIngredientSubstitutionPromptAsync(CancellationToken cancellationToken);
    Task<string> GetIngredientSubstitutionSchemaAsync(CancellationToken cancellationToken);
}