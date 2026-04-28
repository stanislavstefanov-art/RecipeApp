using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.GetRecipe;

public sealed class GetRecipeHandler : IRequestHandler<GetRecipeQuery, ErrorOr<RecipeDto>>
{
    private readonly IRecipeRepository _repository;
    private readonly ICurrentUser _currentUser;

    public GetRecipeHandler(IRecipeRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<RecipeDto>> Handle(GetRecipeQuery request, CancellationToken cancellationToken)
    {
        var recipeId = RecipeId.From(request.Id);
        var recipe = await _repository.GetByIdAsync(recipeId, cancellationToken);

        if (recipe is null)
        {
            return Error.NotFound("Recipe.NotFound", $"Recipe '{request.Id}' was not found.");
        }

        if (recipe.HouseholdId.HasValue)
        {
            var memberIds = await _currentUser.GetHouseholdIdsAsync(cancellationToken);
            if (!memberIds.Contains(recipe.HouseholdId.Value))
            {
                return Error.NotFound("Recipe.NotFound", $"Recipe '{request.Id}' was not found.");
            }
        }

        return new RecipeDto(
            recipe.Id.Value,
            recipe.Name.Value,
            recipe.Ingredients.Select(i => new IngredientDto(i.Name, i.Quantity, i.Unit)).ToList(),
            recipe.Steps.Select(s => new RecipeStepDto(s.Order, s.Instruction)).ToList());
    }
}
