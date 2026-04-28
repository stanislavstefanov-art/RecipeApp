using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.DeleteRecipeRating;

public sealed record DeleteRecipeRatingCommand(Guid RecipeId) : IRequest<ErrorOr<Deleted>>;
