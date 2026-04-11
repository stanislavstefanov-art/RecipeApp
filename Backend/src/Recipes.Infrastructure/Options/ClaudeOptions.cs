namespace Recipes.Infrastructure.Options;

public sealed class ClaudeOptions
{
    public const string SectionName = "Claude";

    public string Model { get; init; } = "claude-sonnet-4-20250514";
    public string ApiKey { get; init; } = string.Empty;
    public int MaxTokens { get; init; } = 2000;
    public string RecipeImportPromptPath { get; init; } = "Docs/Prompts/recipe-import-prompt.md";
    public string RecipeImportSchemaPath { get; init; } = "Docs/recipe-import-schema.json";
    public string MealPlanSuggestionPromptPath { get; init; } = "docs/prompts/meal-plan-suggestion-prompt.md";
    public string MealPlanSuggestionSchemaPath { get; init; } = "docs/meal-plan-suggestion-schema.json";
    public string IngredientSubstitutionPromptPath { get; init; } = "docs/prompts/ingredient-substitution-prompt.md";
    public string IngredientSubstitutionSchemaPath { get; init; } = "docs/ingredient-substitution-schema.json";
}