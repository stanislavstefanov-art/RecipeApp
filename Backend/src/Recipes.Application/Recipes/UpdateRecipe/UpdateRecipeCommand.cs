using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.UpdateRecipe;

public sealed record UpdateRecipeCommand(Guid Id, string Name) : IRequest<ErrorOr<Updated>>;
