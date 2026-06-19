using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.MoveStep;

public sealed class MoveStepHandler : IRequestHandler<MoveStepCommand, ErrorOr<Success>>
{
    private readonly IRecipeRepository _repository;

    public MoveStepHandler(IRecipeRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<Success>> Handle(MoveStepCommand request, CancellationToken cancellationToken)
    {
        var recipe = await _repository.GetByIdAsync(RecipeId.From(request.RecipeId), cancellationToken);
        if (recipe is null)
            return Error.NotFound("Recipe.NotFound", $"Recipe '{request.RecipeId}' was not found.");

        if (!recipe.MoveStep(RecipeStepId.From(request.StepId), request.Direction))
            return Error.NotFound("Step.NotFound", $"Step '{request.StepId}' was not found.");

        await _repository.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
