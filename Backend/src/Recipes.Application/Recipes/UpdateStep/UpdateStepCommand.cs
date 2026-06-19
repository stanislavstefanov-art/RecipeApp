using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.UpdateStep;

public sealed record UpdateStepCommand(Guid RecipeId, Guid StepId, string Instruction)
    : IRequest<ErrorOr<Success>>;
