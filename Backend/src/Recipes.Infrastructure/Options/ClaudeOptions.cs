namespace Recipes.Infrastructure.Options;

public sealed class ClaudeOptions
{
    public const string SectionName = "Claude";

    public string Model { get; init; } = "claude-haiku-4-5-20251001";
    public string ApiKey { get; init; } = string.Empty;
    public int MaxTokens { get; init; } = 2000;
    public string RecipeImportPromptPath { get; init; } = "Docs/Prompts/recipe-import-prompt.md";
    public string RecipeImportSchemaPath { get; init; } = "Docs/recipe-import-schema.json";
    public string MealPlanSuggestionPromptPath { get; init; } = "Docs/Prompts/meal-plan-suggestion-prompt.md";
    public string MealPlanSuggestionSchemaPath { get; init; } = "Docs/meal-plan-suggestion-schema.json";
    public string IngredientSubstitutionPromptPath { get; init; } = "Docs/Prompts/ingredient-substitution-prompt.md";
    public string IngredientSubstitutionSchemaPath { get; init; } = "Docs/ingredient-substitution-schema.json";
    public string ExpenseInsightPromptPath { get; init; } = "Docs/Prompts/expense-insight-prompt.md";
    public string ExpenseInsightSchemaPath { get; init; } = "Docs/expense-insight-schema.json";
    public int MaxContextMessages { get; init; } = 20;
    public int TokenBudgetWarningThreshold { get; init; } = 80_000;
}