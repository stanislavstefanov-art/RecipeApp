using System.Text.Json;
using System.Text.Json.Serialization;
using Recipes.Infrastructure.AI.Claude.Models;

namespace Recipes.Infrastructure.AI.Claude.Agents;

// ── Static tool definitions ────────────────────────────────────────────────

internal static class NutritionAnalysisTools
{
    public static readonly ClaudeToolDefinition GetRecipeIngredients = new(
        "get_recipe_ingredients",
        "Fetch all ingredients for a recipe by its ID. Returns recipe name and a list of ingredients with name, quantity, and unit.",
        JsonDocument.Parse("""
        {
          "type": "object",
          "properties": {
            "recipeId": { "type": "string", "description": "The recipe ID (GUID)." }
          },
          "required": ["recipeId"]
        }
        """).RootElement);

    public static readonly ClaudeToolDefinition SaveNutritionAnalysis = new(
        "save_nutrition_analysis",
        "Save the completed nutrition analysis. Call this after collecting all per-ingredient nutritional data. This is the terminal tool that ends the loop.",
        JsonDocument.Parse("""
        {
          "type": "object",
          "properties": {
            "recipeId":   { "type": "string" },
            "recipeName": { "type": "string" },
            "ingredients": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "name":     { "type": "string" },
                  "quantity": { "type": "number" },
                  "unit":     { "type": "string" },
                  "calories": { "type": "number" },
                  "proteinG": { "type": "number" },
                  "carbG":    { "type": "number" },
                  "fatG":     { "type": "number" },
                  "fiberG":   { "type": "number" }
                },
                "required": ["name", "quantity", "unit", "calories", "proteinG", "carbG", "fatG", "fiberG"]
              }
            },
            "totals": {
              "type": "object",
              "properties": {
                "calories": { "type": "number" },
                "proteinG": { "type": "number" },
                "carbG":    { "type": "number" },
                "fatG":     { "type": "number" },
                "fiberG":   { "type": "number" }
              },
              "required": ["calories", "proteinG", "carbG", "fatG", "fiberG"]
            }
          },
          "required": ["recipeId", "recipeName", "ingredients", "totals"]
        }
        """).RootElement);
}

// ── Tool input / output records ────────────────────────────────────────────

internal sealed record GetRecipeIngredientsInput(
    [property: JsonPropertyName("recipeId")] string RecipeId);

internal sealed record SaveNutritionAnalysisInput(
    [property: JsonPropertyName("recipeId")]   string                            RecipeId,
    [property: JsonPropertyName("recipeName")] string                            RecipeName,
    [property: JsonPropertyName("ingredients")] IReadOnlyList<IngredientNutritionInput> Ingredients,
    [property: JsonPropertyName("totals")]     NutritionTotalsInput              Totals);

internal sealed record IngredientNutritionInput(
    [property: JsonPropertyName("name")]     string  Name,
    [property: JsonPropertyName("quantity")] decimal Quantity,
    [property: JsonPropertyName("unit")]     string  Unit,
    [property: JsonPropertyName("calories")] double  Calories,
    [property: JsonPropertyName("proteinG")] double  ProteinG,
    [property: JsonPropertyName("carbG")]    double  CarbG,
    [property: JsonPropertyName("fatG")]     double  FatG,
    [property: JsonPropertyName("fiberG")]   double  FiberG);

internal sealed record NutritionTotalsInput(
    [property: JsonPropertyName("calories")] double Calories,
    [property: JsonPropertyName("proteinG")] double ProteinG,
    [property: JsonPropertyName("carbG")]    double CarbG,
    [property: JsonPropertyName("fatG")]     double FatG,
    [property: JsonPropertyName("fiberG")]   double FiberG);
