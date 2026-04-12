using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.UpdateRecipeVariationOverrides;

public sealed record UpdateRecipeVariationOverridesCommand(
    Guid RecipeId,
    Guid RecipeVariationId,
    IReadOnlyList<RecipeVariationIngredientOverrideDto> Overrides)
    : IRequest<ErrorOr<Success>>;

public sealed record RecipeVariationIngredientOverrideDto(
    string IngredientName,
    decimal? Quantity,
    string? Unit,
    bool IsRemoved);