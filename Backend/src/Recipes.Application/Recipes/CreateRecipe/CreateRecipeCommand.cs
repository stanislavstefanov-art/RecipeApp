using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.CreateRecipe;

public sealed record CreateRecipeCommand(string Name, Guid HouseholdId) : IRequest<ErrorOr<CreateRecipeResponse>>;

public sealed record CreateRecipeResponse(Guid Id);
