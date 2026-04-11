using Recipes.Application.Recipes.ImportRecipeFromText;

namespace Recipes.Infrastructure.AI.Claude.Models;

public sealed record ClaudeRecipeImportResponse(
    RecipeExtractionResult Result,
    string RawResponse);