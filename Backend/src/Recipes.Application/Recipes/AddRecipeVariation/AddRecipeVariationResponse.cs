namespace Recipes.Application.Recipes.AddRecipeVariation;

public sealed record AddRecipeVariationResponse(
    Guid Id,
    string Name,
    string? Notes,
    string? IngredientAdjustmentNotes);