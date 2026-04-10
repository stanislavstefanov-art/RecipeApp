using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.DeleteRecipe;

public sealed record DeleteRecipeCommand(Guid Id) : IRequest<ErrorOr<Deleted>>;
