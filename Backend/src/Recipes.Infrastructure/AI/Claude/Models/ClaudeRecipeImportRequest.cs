namespace Recipes.Infrastructure.AI.Claude.Models;

public sealed record ClaudeRecipeImportRequest(
    string Model,
    int MaxTokens,
    string SystemPrompt,
    string UserText,
    string JsonSchema);