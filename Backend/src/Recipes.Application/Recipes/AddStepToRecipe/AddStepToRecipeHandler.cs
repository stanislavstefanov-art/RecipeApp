using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.AddStepToRecipe;

public sealed class AddStepToRecipeHandler : IRequestHandler<AddStepToRecipeCommand, ErrorOr<Success>>
{
    private readonly IRecipeRepository _repository;

    public AddStepToRecipeHandler(IRecipeRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<Success>> Handle(
        AddStepToRecipeCommand request,
        CancellationToken cancellationToken)
    {
        var recipeId = RecipeId.From(request.RecipeId);

        var recipe = await _repository.GetByIdAsync(recipeId, cancellationToken);

        if (recipe is null)
        {
            return Error.NotFound("Recipe.NotFound", $"Recipe '{request.RecipeId}' was not found.");
        }

        recipe.AddStep(request.Instruction);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
