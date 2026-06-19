using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.MoveStep;

public sealed record MoveStepCommand(Guid RecipeId, Guid StepId, string Direction)
    : IRequest<ErrorOr<Success>>;
