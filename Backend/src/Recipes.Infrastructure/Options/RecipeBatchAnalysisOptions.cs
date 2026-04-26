namespace Recipes.Infrastructure.Options;

public sealed class RecipeBatchAnalysisOptions
{
    public const string SectionName = "RecipeBatchAnalysis";

    public string Provider { get; init; } = "Stub"; // "Stub" or "Claude"
}
