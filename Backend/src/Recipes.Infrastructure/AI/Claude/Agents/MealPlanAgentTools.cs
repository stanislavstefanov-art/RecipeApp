using System.Text.Json;
using Recipes.Infrastructure.AI.Claude.Models;

namespace Recipes.Infrastructure.AI.Claude.Agents;

internal static class MealPlanAgentTools
{
    // ── Orchestrator tools ─────────────────────────────────────────────────

    internal static readonly ClaudeToolDefinition DecomposeTask = new(
        Name: "decompose_task",
        Description: """
            Analyse the household's profile and produce a task decomposition that
            guides the recipe discovery and assignment sub-agents.
            Call this exactly once to structure the planning approach.
            """,
        InputSchema: JsonDocument.Parse("""
            {
              "type": "object",
              "required": ["recipeFocus", "diversityGoal", "servingsPerMeal"],
              "additionalProperties": false,
              "properties": {
                "recipeFocus": {
                  "type": "string",
                  "description": "Brief guidance on recipe style, e.g. 'varied, Mediterranean-leaning dishes with vegetarian options'."
                },
                "diversityGoal": {
                  "type": "string",
                  "description": "How much variety is desired across the week, e.g. 'no recipe repeated; mix cuisines'."
                },
                "servingsPerMeal": {
                  "type": "integer",
                  "description": "Typical serving count per meal based on household size."
                }
              }
            }
            """).RootElement);

    // ── RecipeDiscoverySubAgent tools ──────────────────────────────────────

    internal static readonly ClaudeToolDefinition ListAvailableRecipes = new(
        Name: "list_available_recipes",
        Description: "Return all recipes in the system as a list of {id, name} pairs. Call this first to see what is available.",
        InputSchema: JsonDocument.Parse("""{"type":"object","properties":{}}""").RootElement);

    internal static readonly ClaudeToolDefinition GetRecipeDetails = new(
        Name: "get_recipe_details",
        Description: "Return full details of a recipe: servings, ingredients. Use to check suitability for dietary preferences.",
        InputSchema: JsonDocument.Parse("""
            {
              "type": "object",
              "required": ["recipeId"],
              "additionalProperties": false,
              "properties": {
                "recipeId": {"type": "string", "description": "Recipe UUID."}
              }
            }
            """).RootElement);

    internal static readonly ClaudeToolDefinition GetRecentMealHistory = new(
        Name: "get_recent_meal_history",
        Description: "Return recipe IDs used in the household's meal plans within the last N days. Use to avoid repetition.",
        InputSchema: JsonDocument.Parse("""
            {
              "type": "object",
              "required": ["days"],
              "additionalProperties": false,
              "properties": {
                "days": {"type": "integer", "description": "How many days back to look (e.g. 14)."}
              }
            }
            """).RootElement);

    internal static readonly ClaudeToolDefinition SubmitCandidates = new(
        Name: "submit_candidates",
        Description: """
            Submit the final list of recipe candidates for the meal plan.
            Include at least (days × mealTypes) candidates so the assignment agent has
            enough variety. Each entry should explain why the recipe fits.
            Calling this ends the discovery loop.
            """,
        InputSchema: JsonDocument.Parse("""
            {
              "type": "object",
              "required": ["candidates"],
              "additionalProperties": false,
              "properties": {
                "candidates": {
                  "type": "array",
                  "minItems": 1,
                  "items": {
                    "type": "object",
                    "required": ["recipeId", "recipeName", "suitability"],
                    "additionalProperties": false,
                    "properties": {
                      "recipeId":   {"type": "string"},
                      "recipeName": {"type": "string"},
                      "suitability": {"type": "string", "description": "Why this recipe is a good fit."}
                    }
                  }
                }
              }
            }
            """).RootElement);

    internal static IReadOnlyList<ClaudeToolDefinition> DiscoveryTools =>
    [
        ListAvailableRecipes,
        GetRecipeDetails,
        GetRecentMealHistory,
        SubmitCandidates,
    ];

    // ── MealAssignmentSubAgent tools ───────────────────────────────────────

    internal static readonly ClaudeToolDefinition AssignMeal = new(
        Name: "assign_meal",
        Description: """
            Assign one recipe to a specific day and meal type, and list which household
            members it is for. Call once per day × mealType slot.
            """,
        InputSchema: JsonDocument.Parse("""
            {
              "type": "object",
              "required": ["date", "mealType", "recipeId", "personIds"],
              "additionalProperties": false,
              "properties": {
                "date":      {"type": "string", "format": "date", "description": "ISO-8601 date, e.g. '2026-04-28'."},
                "mealType":  {"type": "integer", "description": "Meal type code matching the plan's mealTypes array."},
                "recipeId":  {"type": "string",  "description": "UUID of the chosen recipe from the candidate list."},
                "personIds": {
                  "type": "array",
                  "items": {"type": "string"},
                  "description": "UUIDs of household members this meal is for."
                }
              }
            }
            """).RootElement);

    internal static readonly ClaudeToolDefinition FinalizeAssignments = new(
        Name: "finalize_assignments",
        Description: """
            Signal that all day × mealType slots have been assigned.
            The system will validate coverage and return an error if any slots are missing.
            Call this only after assigning every slot.
            """,
        InputSchema: JsonDocument.Parse("""{"type":"object","properties":{}}""").RootElement);

    internal static IReadOnlyList<ClaudeToolDefinition> AssignmentTools =>
    [
        AssignMeal,
        FinalizeAssignments,
    ];
}
