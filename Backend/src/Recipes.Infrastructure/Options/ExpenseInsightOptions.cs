namespace Recipes.Infrastructure.Options;

public sealed class ExpenseInsightOptions
{
    public const string SectionName = "ExpenseInsight";

    public string Provider { get; init; } = "Stub"; // "Stub" or "Claude"
}
