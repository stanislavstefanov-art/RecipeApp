namespace Recipes.Infrastructure.Options;

public sealed class RecipeCritiqueOptions
{
    public const string SectionName = "RecipeCritique";

    public string Provider { get; init; } = "Stub"; // "Stub" or "Claude"
}
