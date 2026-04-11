using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Recipes.Application.Common.AI;
using Recipes.Application.Recipes.ImportRecipeFromText;
using Recipes.Infrastructure.Options;

namespace Recipes.Infrastructure.AI.Claude.Services;

public sealed class ClaudeRecipeImportService : IRecipeImportService
{
    private readonly ClaudeOptions _options;
    private readonly ILogger<ClaudeRecipeImportService> _logger;
    private readonly IClaudeAssetProvider _assetProvider;
    private readonly IClaudeRecipeImportClient _client;

    public ClaudeRecipeImportService(
        IOptions<ClaudeOptions> options,
        ILogger<ClaudeRecipeImportService> logger,
        IClaudeAssetProvider assetProvider,
        IClaudeRecipeImportClient client)
    {
        _options = options.Value;
        _logger = logger;
        _assetProvider = assetProvider;
        _client = client;
    }

    public async Task<RecipeExtractionResult> ImportAsync(string text, CancellationToken cancellationToken)
    {
        var prompt = await _assetProvider.GetRecipeImportPromptAsync(cancellationToken);
        var schema = await _assetProvider.GetRecipeImportSchemaAsync(cancellationToken);

        _logger.LogInformation(
            "Claude recipe import requested for text length {Length} using model {Model}.",
            text.Length,
            _options.Model);

        return await _client.ImportAsync(text, prompt, schema, cancellationToken);
    }
}