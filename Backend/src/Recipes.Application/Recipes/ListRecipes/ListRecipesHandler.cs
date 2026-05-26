using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.ListRecipes;

public sealed class ListRecipesHandler : IRequestHandler<ListRecipesQuery, ErrorOr<IReadOnlyList<RecipeListItemDto>>>
{
    private readonly IRecipeRepository _repository;
    private readonly ICurrentUser _currentUser;

    public ListRecipesHandler(IRecipeRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<IReadOnlyList<RecipeListItemDto>>> Handle(
        ListRecipesQuery request,
        CancellationToken cancellationToken)
    {
        var householdIds = await _currentUser.GetHouseholdIdsAsync(cancellationToken);
        var recipes = await _repository.GetByHouseholdIdsAsync(householdIds, cancellationToken);

        IReadOnlyList<RecipeListItemDto> result = recipes
            .Select(r => new RecipeListItemDto(
                r.Id.Value,
                r.Name.Value,
                r.AverageStars,
                r.RatingCount,
                r.ImageUrl,
                (int)r.RecipeType,
                r.IsImported,
                r.Ingredients.Select(i => i.Name).ToList()))
            .ToList();

        return result.ToErrorOr();
    }
}
