using MediatR;

namespace Recipes.Application.Recipes.SearchRecipesByIngredient;

public sealed record SearchRecipesByIngredientQuery(string Ingredient)
    : IRequest<IReadOnlyList<RecipeSearchResultDto>>;

public sealed record RecipeSearchResultDto(Guid Id, string Name);

