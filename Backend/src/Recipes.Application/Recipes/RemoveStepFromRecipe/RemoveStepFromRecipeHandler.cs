using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.RemoveStepFromRecipe;

public sealed class RemoveStepFromRecipeHandler : IRequestHandler<RemoveStepFromRecipeCommand, ErrorOr<Success>>
{
    private readonly IRecipeRepository _repository;

    public RemoveStepFromRecipeHandler(IRecipeRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<Success>> Handle(
        RemoveStepFromRecipeCommand request,
        CancellationToken cancellationToken)
    {
        var recipeId = RecipeId.From(request.RecipeId);
        var recipe = await _repository.GetByIdAsync(recipeId, cancellationToken);

        if (recipe is null)
            return Error.NotFound("Recipe.NotFound", $"Recipe '{request.RecipeId}' was not found.");

        var stepId = RecipeStepId.From(request.StepId);
        var removed = recipe.RemoveStep(stepId);
        if (!removed)
            return Error.NotFound("RecipeStep.NotFound", $"Step '{request.StepId}' was not found.");

        await _repository.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
