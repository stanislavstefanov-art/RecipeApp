using ErrorOr;
using MediatR;
using Recipes.Application.Recipes;

namespace Recipes.Application.Recipes.RateRecipe;

public sealed record RateRecipeCommand(Guid RecipeId, int Stars, string? Comment)
    : IRequest<ErrorOr<RecipeRatingDto>>;
