using System.Text.Json;
using Recipes.Infrastructure.AI.Claude.Models;

namespace Recipes.Infrastructure.AI.Claude.Agents;

internal static class RecipeImportTools
{
    internal static readonly ClaudeToolDefinition FetchUrlContent = new(
        Name: "fetch_url_content",
        Description: """
            Fetch the HTML content from a URL and return it as cleaned plain text.
            Call this first to get the raw recipe page content.
            Returns { "content": "..." } on success.
            Returns is_error: true with { "error": "..." } on network failure or non-2xx response.
            """,
        InputSchema: JsonDocument.Parse("""
            {
              "type": "object",
              "required": ["url"],
              "additionalProperties": false,
              "properties": {
                "url": {
                  "type": "string",
                  "description": "Absolute http or https URL to fetch."
                }
              }
            }
            """).RootElement
    );

    internal static readonly ClaudeToolDefinition ExtractRecipeFields = new(
        Name: "extract_recipe_fields",
        Description: """
            Submit your initial extraction of recipe fields from the fetched text.
            Validates the structure and reports the number of ingredients to normalize.
            Returns { "ingredientCount": N, "message": "..." }.
            Returns is_error: true if ingredients or steps are missing.
            """,
        InputSchema: JsonDocument.Parse("""
            {
              "type": "object",
              "required": ["title", "servings", "ingredients", "steps"],
              "additionalProperties": false,
              "properties": {
                "title":    { "type": ["string", "null"] },
                "servings": { "type": ["integer", "null"], "minimum": 1 },
                "ingredients": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "required": ["name"],
                    "additionalProperties": false,
                    "properties": {
                      "name":        { "type": "string" },
                      "rawQuantity": { "type": ["string", "null"], "description": "Free-form quantity, e.g. '2½' or '3/4'." },
                      "rawUnit":     { "type": ["string", "null"], "description": "Free-form unit, e.g. 'tablespoons'." },
                      "notes":       { "type": ["string", "null"] }
                    }
                  }
                },
                "steps": { "type": "array", "items": { "type": "string" } },
                "notes": { "type": ["string", "null"] }
              }
            }
            """).RootElement
    );

    internal static readonly ClaudeToolDefinition NormalizeIngredient = new(
        Name: "normalize_ingredient",
        Description: """
            Normalize a single ingredient's name, quantity, and unit into canonical form.
            Call this in parallel for every ingredient returned by extract_recipe_fields.
            Returns { "normalizedName": "...", "quantity": 2.5, "unit": "tbsp" }.
            Never errors — unknown units pass through unchanged.
            """,
        InputSchema: JsonDocument.Parse("""
            {
              "type": "object",
              "required": ["name"],
              "additionalProperties": false,
              "properties": {
                "name":        { "type": "string",          "description": "Ingredient name as extracted." },
                "rawQuantity": { "type": ["string", "null"], "description": "Free-form quantity text." },
                "rawUnit":     { "type": ["string", "null"], "description": "Free-form unit text." }
              }
            }
            """).RootElement
    );

    internal static readonly ClaudeToolDefinition SaveRecipe = new(
        Name: "save_recipe",
        Description: """
            Submit the final recipe with all normalized ingredients. This is the last
            tool call — invoke it once all ingredients are normalized.
            Returns { "status": "saved" }.
            """,
        InputSchema: JsonDocument.Parse("""
            {
              "type": "object",
              "required": ["title", "ingredients", "steps", "confidence", "needsReview"],
              "additionalProperties": false,
              "properties": {
                "title":    { "type": ["string", "null"] },
                "servings": { "type": ["integer", "null"], "minimum": 1 },
                "ingredients": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "required": ["name"],
                    "additionalProperties": false,
                    "properties": {
                      "name":     { "type": "string" },
                      "quantity": { "type": ["number", "null"] },
                      "unit":     { "type": ["string", "null"] },
                      "notes":    { "type": ["string", "null"] }
                    }
                  }
                },
                "steps":       { "type": "array", "items": { "type": "string" } },
                "notes":       { "type": ["string", "null"] },
                "confidence":  { "type": "number", "minimum": 0, "maximum": 1 },
                "needsReview": { "type": "boolean" }
              }
            }
            """).RootElement
    );

    internal static readonly IReadOnlyList<ClaudeToolDefinition> All =
        [FetchUrlContent, ExtractRecipeFields, NormalizeIngredient, SaveRecipe];
}
