namespace Recipes.Application.Recipes.BatchAnalyseRecipes;

public sealed record BatchSubmissionDto(
    string BatchId,
    string Status,
    int RequestCount,
    Guid ProvenanceId);

public sealed record BatchResultsDto(
    string BatchId,
    string ProcessingStatus,
    IReadOnlyList<RecipeAnalysisResultDto>? Results,
    BatchCacheStatsDto? CacheStats);

public sealed record RecipeAnalysisResultDto(
    Guid RecipeId,
    string RecipeName,
    bool Succeeded,
    int? QualityScore,
    string? Summary,
    string? ErrorMessage);

public sealed record BatchCacheStatsDto(
    int TotalInputTokens,
    int TotalOutputTokens,
    int CacheCreationTokens,
    int CacheReadTokens);
