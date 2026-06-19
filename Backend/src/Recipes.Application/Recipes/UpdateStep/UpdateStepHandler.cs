using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.UpdateStep;

public sealed class UpdateStepHandler : IRequestHandler<UpdateStepCommand, ErrorOr<Success>>
{
    private readonly IRecipeRepository _repository;

    public UpdateStepHandler(IRecipeRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<Success>> Handle(UpdateStepCommand request, CancellationToken cancellationToken)
    {
        var recipe = await _repository.GetByIdAsync(RecipeId.From(request.RecipeId), cancellationToken);
        if (recipe is null)
            return Error.NotFound("Recipe.NotFound", $"Recipe '{request.RecipeId}' was not found.");

        if (!recipe.UpdateStep(RecipeStepId.From(request.StepId), request.Instruction))
            return Error.NotFound("Step.NotFound", $"Step '{request.StepId}' was not found.");

        await _repository.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
