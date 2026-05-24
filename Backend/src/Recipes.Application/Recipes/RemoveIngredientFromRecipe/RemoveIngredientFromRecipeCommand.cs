using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.RemoveIngredientFromRecipe;

public sealed record RemoveIngredientFromRecipeCommand(Guid RecipeId, Guid IngredientId) : IRequest<ErrorOr<Success>>;
