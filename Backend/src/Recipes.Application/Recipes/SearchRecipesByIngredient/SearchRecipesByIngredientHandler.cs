using ErrorOr;
using MediatR;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.SearchRecipesByIngredient;

public sealed class SearchRecipesByIngredientHandler
    : IRequestHandler<SearchRecipesByIngredientQuery, ErrorOr<IReadOnlyList<RecipeSearchResultDto>>>
{
    private readonly IRecipeRepository _repository;

    public SearchRecipesByIngredientHandler(IRecipeRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<IReadOnlyList<RecipeSearchResultDto>>> Handle(
        SearchRecipesByIngredientQuery request,
        CancellationToken cancellationToken)
    {
        var recipes = await _repository.SearchByIngredientNameAsync(
            request.Ingredient.Trim(),
            cancellationToken);

        IReadOnlyList<RecipeSearchResultDto> result = recipes
            .Select(r => new RecipeSearchResultDto(r.Id.Value, r.Name.Value))
            .ToList();

        return result.ToErrorOr();
    }
}
