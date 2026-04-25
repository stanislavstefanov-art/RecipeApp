using System.Text.Json.Serialization;
using Recipes.Application.MealPlans.SuggestMealPlan;

namespace Recipes.Infrastructure.AI.Claude.Agents;

// ── Orchestrator decomposition output ─────────────────────────────────────

internal sealed record TaskDecomposition(string RecipeFocus, string DiversityGoal, int ServingsPerMeal);

// ── Context records (explicit handoff contracts between agents) ────────────

internal sealed record RecipeDiscoveryContext(
    HouseholdPlanningProfileDto Household,
    TaskDecomposition Decomposition,
    int DaysCount,
    IReadOnlyList<int> MealTypes);

internal sealed record RecipeCandidate(Guid RecipeId, string RecipeName, string Suitability);

internal sealed record MealAssignmentContext(
    IReadOnlyList<RecipeCandidate> Candidates,
    HouseholdPlanningProfileDto Household,
    DateOnly StartDate,
    int DaysCount,
    IReadOnlyList<int> MealTypes);

// ── Tool input deserialisation targets ────────────────────────────────────

internal sealed record DecomposeTaskInput(
    [property: JsonPropertyName("recipeFocus")]    string RecipeFocus,
    [property: JsonPropertyName("diversityGoal")] string DiversityGoal,
    [property: JsonPropertyName("servingsPerMeal")] int   ServingsPerMeal);

internal sealed record GetRecipeDetailsInput(
    [property: JsonPropertyName("recipeId")] string RecipeId);

internal sealed record GetRecentMealHistoryInput(
    [property: JsonPropertyName("days")] int Days);

internal sealed record SubmitCandidatesInput(
    [property: JsonPropertyName("candidates")] IReadOnlyList<CandidateItem> Candidates);

internal sealed record CandidateItem(
    [property: JsonPropertyName("recipeId")]   string RecipeId,
    [property: JsonPropertyName("recipeName")] string RecipeName,
    [property: JsonPropertyName("suitability")] string Suitability);

internal sealed record AssignMealInput(
    [property: JsonPropertyName("date")]      string             Date,
    [property: JsonPropertyName("mealType")]  int                MealType,
    [property: JsonPropertyName("recipeId")]  string             RecipeId,
    [property: JsonPropertyName("personIds")] IReadOnlyList<string> PersonIds);
