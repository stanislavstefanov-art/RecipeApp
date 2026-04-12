using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.AddRecipeVariation;

public sealed class AddRecipeVariationHandler
    : IRequestHandler<AddRecipeVariationCommand, ErrorOr<AddRecipeVariationResponse>>
{
    private readonly IRecipeRepository _recipeRepository;

    public AddRecipeVariationHandler(IRecipeRepository recipeRepository)
    {
        _recipeRepository = recipeRepository;
    }

    public async Task<ErrorOr<AddRecipeVariationResponse>> Handle(
        AddRecipeVariationCommand request,
        CancellationToken cancellationToken)
    {
        var recipe = await _recipeRepository.GetByIdAsync(
            RecipeId.From(request.RecipeId),
            cancellationToken);

        if (recipe is null)
        {
            return Error.NotFound("Recipe.NotFound", $"Recipe '{request.RecipeId}' was not found.");
        }

        try
        {
            var variation = recipe.AddVariation(
                request.Name,
                request.Notes,
                request.IngredientAdjustmentNotes);

            await _recipeRepository.SaveChangesAsync(cancellationToken);

            return new AddRecipeVariationResponse(
                variation.Id.Value,
                variation.Name,
                variation.Notes,
                variation.IngredientAdjustmentNotes);
        }
        catch (InvalidOperationException ex)
        {
            return Error.Conflict("RecipeVariation.AlreadyExists", ex.Message);
        }
    }
}