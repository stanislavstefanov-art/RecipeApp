using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.ListRecipes;

public sealed record ListRecipesQuery : IRequest<ErrorOr<IReadOnlyList<RecipeListItemDto>>>;

public sealed record RecipeListItemDto(Guid Id, string Name, double? AverageStars, int RatingCount, string? ImageUrl, int RecipeType, bool IsImported, IReadOnlyList<string> IngredientNames, int Origin, int MealsPerCook, IReadOnlyList<int> AppropriateForMealTypes);
