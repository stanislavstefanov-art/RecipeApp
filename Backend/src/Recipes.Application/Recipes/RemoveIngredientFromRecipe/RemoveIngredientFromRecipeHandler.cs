using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.RemoveIngredientFromRecipe;

public sealed class RemoveIngredientFromRecipeHandler : IRequestHandler<RemoveIngredientFromRecipeCommand, ErrorOr<Success>>
{
    private readonly IRecipeRepository _repository;

    public RemoveIngredientFromRecipeHandler(IRecipeRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<Success>> Handle(
        RemoveIngredientFromRecipeCommand request,
        CancellationToken cancellationToken)
    {
        var recipeId = RecipeId.From(request.RecipeId);
        var recipe = await _repository.GetByIdAsync(recipeId, cancellationToken);

        if (recipe is null)
            return Error.NotFound("Recipe.NotFound", $"Recipe '{request.RecipeId}' was not found.");

        var ingredientId = RecipeIngredientId.From(request.IngredientId);
        var removed = recipe.RemoveIngredient(ingredientId);
        if (!removed)
            return Error.NotFound("Ingredient.NotFound", $"Ingredient '{request.IngredientId}' was not found.");

        await _repository.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
