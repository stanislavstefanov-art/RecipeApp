namespace Recipes.Infrastructure.Options;

public sealed class RecipeImportOptions
{
    public const string SectionName = "RecipeImport";

    public string Provider { get; init; } = "Stub"; // "Stub" or "Claude"
}