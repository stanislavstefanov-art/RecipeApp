using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.AddRecipeVariation;

public sealed record AddRecipeVariationCommand(
    Guid RecipeId,
    string Name,
    string? Notes,
    string? IngredientAdjustmentNotes) : IRequest<ErrorOr<AddRecipeVariationResponse>>;