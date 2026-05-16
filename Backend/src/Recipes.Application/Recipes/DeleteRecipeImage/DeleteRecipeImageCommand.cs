using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.DeleteRecipeImage;

public sealed record DeleteRecipeImageCommand(Guid RecipeId) : IRequest<ErrorOr<Deleted>>;
