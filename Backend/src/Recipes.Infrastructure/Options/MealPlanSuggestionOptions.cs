namespace Recipes.Infrastructure.Options;

public sealed class MealPlanSuggestionOptions
{
    public const string SectionName = "MealPlanSuggestion";

    public string Provider { get; init; } = "Stub"; // "Stub" or "Claude"
}