using System.Collections.Concurrent;
using Recipes.Application.Recipes.BatchAnalyseRecipes;
using Recipes.Application.Recipes.GetRecipe;

namespace Recipes.Infrastructure.AI.Claude.Services.Stubs;

public sealed class StubRecipeBatchAnalysisService : IRecipeBatchAnalysisService
{
    private static readonly ConcurrentDictionary<string, IReadOnlyList<RecipeDto>> SubmittedBatches = new();

    public Task<BatchSubmissionDto> SubmitBatchAsync(
        IReadOnlyList<RecipeDto> recipes,
        CancellationToken cancellationToken)
    {
        var batchId = $"stub_batch_{Guid.NewGuid():N}";
        SubmittedBatches[batchId] = recipes;

        var submission = new BatchSubmissionDto(
            BatchId: batchId,
            Status: "ended",
            RequestCount: recipes.Count,
            ProvenanceId: Guid.Empty);

        return Task.FromResult(submission);
    }

    public Task<BatchResultsDto> GetResultsAsync(string batchId, CancellationToken cancellationToken)
    {
        if (!SubmittedBatches.TryGetValue(batchId, out var recipes))
        {
            var empty = new BatchResultsDto(batchId, "not_found", null, null);
            return Task.FromResult(empty);
        }

        var analysis = recipes
            .Select(r => new RecipeAnalysisResultDto(
                RecipeId: r.Id,
                RecipeName: r.Name,
                Succeeded: true,
                QualityScore: 75,
                Summary: $"Stub analysis: '{r.Name}' is a hearty weeknight option with {r.Ingredients.Count} ingredients and {r.Steps.Count} steps.",
                ErrorMessage: null))
            .ToList();

        var cacheStats = new BatchCacheStatsDto(
            TotalInputTokens: 0,
            TotalOutputTokens: 0,
            CacheCreationTokens: 0,
            CacheReadTokens: 0);

        var result = new BatchResultsDto(
            BatchId: batchId,
            ProcessingStatus: "ended",
            Results: analysis,
            CacheStats: cacheStats);

        return Task.FromResult(result);
    }
}
