using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.AddIngredientToRecipe;

public sealed record AddIngredientToRecipeCommand(
    Guid RecipeId,
    string Name,
    decimal Quantity,
    string Unit) : IRequest<ErrorOr<Success>>;
