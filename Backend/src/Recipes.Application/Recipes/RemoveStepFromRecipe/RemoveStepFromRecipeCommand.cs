using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.RemoveStepFromRecipe;

public sealed record RemoveStepFromRecipeCommand(Guid RecipeId, Guid StepId) : IRequest<ErrorOr<Success>>;
