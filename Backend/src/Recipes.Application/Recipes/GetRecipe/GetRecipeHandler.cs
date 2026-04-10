using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.GetRecipe;

public sealed class GetRecipeHandler : IRequestHandler<GetRecipeQuery, ErrorOr<RecipeDto>>
{
    private readonly IRecipeRepository _repository;

    public GetRecipeHandler(IRecipeRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<RecipeDto>> Handle(GetRecipeQuery request, CancellationToken cancellationToken)
    {
        var recipeId = RecipeId.From(request.Id);

        var recipe = await _repository.GetByIdAsync(recipeId, cancellationToken);

        if (recipe is null)
        {
            return Error.NotFound("Recipe.NotFound", $"Recipe '{request.Id}' was not found.");
        }

        return new RecipeDto(
            recipe.Id.Value,
            recipe.Name.Value,
            recipe.Ingredients.Select(i => new IngredientDto(i.Name, i.Quantity, i.Unit)).ToList(),
            recipe.Steps.Select(s => new RecipeStepDto(s.Order, s.Instruction)).ToList());
    }
}
