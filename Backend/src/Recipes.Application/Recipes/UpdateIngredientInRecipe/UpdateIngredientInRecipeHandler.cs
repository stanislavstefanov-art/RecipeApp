using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.UpdateIngredientInRecipe;

public sealed class UpdateIngredientInRecipeHandler : IRequestHandler<UpdateIngredientInRecipeCommand, ErrorOr<Success>>
{
    private readonly IRecipeRepository _repository;

    public UpdateIngredientInRecipeHandler(IRecipeRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<Success>> Handle(
        UpdateIngredientInRecipeCommand request,
        CancellationToken cancellationToken)
    {
        var recipeId = RecipeId.From(request.RecipeId);
        var recipe = await _repository.GetByIdAsync(recipeId, cancellationToken);

        if (recipe is null)
            return Error.NotFound("Recipe.NotFound", $"Recipe '{request.RecipeId}' was not found.");

        var ingredientId = RecipeIngredientId.From(request.IngredientId);
        var updated = recipe.UpdateIngredient(ingredientId, request.Name, request.Quantity, request.Unit);
        if (!updated)
            return Error.NotFound("Ingredient.NotFound", $"Ingredient '{request.IngredientId}' was not found.");

        await _repository.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
