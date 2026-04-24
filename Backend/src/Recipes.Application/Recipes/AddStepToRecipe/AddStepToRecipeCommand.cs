using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.AddStepToRecipe;

public sealed record AddStepToRecipeCommand(
    Guid RecipeId,
    string Instruction) : IRequest<ErrorOr<Success>>;
