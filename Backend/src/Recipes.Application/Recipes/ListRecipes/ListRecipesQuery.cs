using MediatR;

namespace Recipes.Application.Recipes.ListRecipes;

public sealed record ListRecipesQuery : IRequest<IReadOnlyList<RecipeListItemDto>>;

public sealed record RecipeListItemDto(Guid Id, string Name);

