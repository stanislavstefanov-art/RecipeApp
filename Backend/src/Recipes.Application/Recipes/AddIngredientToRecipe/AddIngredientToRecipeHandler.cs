using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.AddIngredientToRecipe;

public sealed class AddIngredientToRecipeHandler : IRequestHandler<AddIngredientToRecipeCommand, ErrorOr<Success>>
{
    private readonly IRecipeRepository _repository;

    public AddIngredientToRecipeHandler(IRecipeRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<Success>> Handle(
        AddIngredientToRecipeCommand request,
        CancellationToken cancellationToken)
    {
        var recipeId = RecipeId.From(request.RecipeId);

        var recipe = await _repository.GetByIdAsync(recipeId, cancellationToken);

        if (recipe is null)
        {
            return Error.NotFound("Recipe.NotFound", $"Recipe '{request.RecipeId}' was not found.");
        }

        recipe.AddIngredient(request.Name, request.Quantity, request.Unit);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
