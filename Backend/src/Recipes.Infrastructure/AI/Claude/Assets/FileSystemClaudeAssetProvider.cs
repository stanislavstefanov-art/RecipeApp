using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Recipes.Application.Common.AI;
using Recipes.Infrastructure.Options;

namespace Recipes.Infrastructure.AI.Claude.Assets;

public sealed class FileSystemClaudeAssetProvider : IClaudeAssetProvider
{
    private readonly ClaudeOptions _options;
    private readonly ILogger<FileSystemClaudeAssetProvider> _logger;

    public FileSystemClaudeAssetProvider(
        IOptions<ClaudeOptions> options,
        ILogger<FileSystemClaudeAssetProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<string> GetRecipeImportPromptAsync(CancellationToken cancellationToken)
        => ReadAsync(_options.RecipeImportPromptPath, "recipe import prompt", cancellationToken);

    public Task<string> GetRecipeImportSchemaAsync(CancellationToken cancellationToken)
        => ReadAsync(_options.RecipeImportSchemaPath, "recipe import schema", cancellationToken);

    public Task<string> GetMealPlanSuggestionPromptAsync(CancellationToken cancellationToken)
        => ReadAsync(_options.MealPlanSuggestionPromptPath, "meal plan suggestion prompt", cancellationToken);

    public Task<string> GetMealPlanSuggestionSchemaAsync(CancellationToken cancellationToken)
        => ReadAsync(_options.MealPlanSuggestionSchemaPath, "meal plan suggestion schema", cancellationToken);

    public Task<string> GetIngredientSubstitutionPromptAsync(CancellationToken cancellationToken)
        => ReadAsync(_options.IngredientSubstitutionPromptPath, "ingredient substitution prompt", cancellationToken);

    public Task<string> GetIngredientSubstitutionSchemaAsync(CancellationToken cancellationToken)
        => ReadAsync(_options.IngredientSubstitutionSchemaPath, "ingredient substitution schema", cancellationToken);

    private async Task<string> ReadAsync(string relativePath, string description, CancellationToken cancellationToken)
    {
        var path = ResolvePath(relativePath);
        _logger.LogInformation("Loading {Description} from {Path}", description, path);
        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    private static string ResolvePath(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativePath));

        if (File.Exists(fullPath))
        {
            return fullPath;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", relativePath));
    }
}