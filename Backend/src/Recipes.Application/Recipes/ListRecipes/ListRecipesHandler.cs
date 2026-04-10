using ErrorOr;
using MediatR;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.ListRecipes;

public sealed class ListRecipesHandler : IRequestHandler<ListRecipesQuery, ErrorOr<IReadOnlyList<RecipeListItemDto>>>
{
    private readonly IRecipeRepository _repository;

    public ListRecipesHandler(IRecipeRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<IReadOnlyList<RecipeListItemDto>>> Handle(
        ListRecipesQuery request,
        CancellationToken cancellationToken)
    {
        var recipes = await _repository.GetAllAsync(cancellationToken);

        IReadOnlyList<RecipeListItemDto> result = recipes
            .Select(r => new RecipeListItemDto(r.Id.Value, r.Name.Value))
            .ToList();

        return result.ToErrorOr();
    }
}
