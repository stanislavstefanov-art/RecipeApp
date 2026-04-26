namespace Recipes.Infrastructure.Options;

public sealed class RecipeScalingOptions
{
    public const string SectionName = "RecipeScaling";

    public string Provider { get; init; } = "Stub"; // "Stub" or "Claude"
}
