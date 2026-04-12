using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.UpdateRecipeVariationOverrides;

public sealed class UpdateRecipeVariationOverridesHandler
    : IRequestHandler<UpdateRecipeVariationOverridesCommand, ErrorOr<Success>>
{
    private readonly IRecipeRepository _recipeRepository;

    public UpdateRecipeVariationOverridesHandler(IRecipeRepository recipeRepository)
    {
        _recipeRepository = recipeRepository;
    }

    public async Task<ErrorOr<Success>> Handle(
        UpdateRecipeVariationOverridesCommand request,
        CancellationToken cancellationToken)
    {
        var recipe = await _recipeRepository.GetByIdAsync(
            RecipeId.From(request.RecipeId),
            cancellationToken);

        if (recipe is null)
        {
            return Error.NotFound("Recipe.NotFound", $"Recipe '{request.RecipeId}' was not found.");
        }

        var variation = recipe.Variations.SingleOrDefault(v => v.Id == RecipeVariationId.From(request.RecipeVariationId));
        if (variation is null)
        {
            return Error.NotFound(
                "RecipeVariation.NotFound",
                $"Recipe variation '{request.RecipeVariationId}' was not found.");
        }

        foreach (var item in request.Overrides)
        {
            if (item.IsRemoved)
            {
                variation.RemoveIngredient(item.IngredientName);
            }
            else if (item.Quantity.HasValue && !string.IsNullOrWhiteSpace(item.Unit))
            {
                variation.OverrideIngredient(item.IngredientName, item.Quantity.Value, item.Unit);
            }
        }

        await _recipeRepository.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}