namespace Recipes.Infrastructure.Options;

public sealed class IngredientSubstitutionOptions
{
    public const string SectionName = "IngredientSubstitution";

    public string Provider { get; init; } = "Stub"; // "Stub" or "Claude"
}