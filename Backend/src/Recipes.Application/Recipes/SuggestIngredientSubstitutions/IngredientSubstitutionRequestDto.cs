namespace Recipes.Application.Recipes.SuggestIngredientSubstitutions;

public sealed record IngredientSubstitutionRequestDto(
    string IngredientName,
    string? RecipeContext,
    string? DietaryGoal);