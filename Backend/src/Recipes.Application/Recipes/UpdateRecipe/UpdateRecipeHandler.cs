using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.UpdateRecipe;

public sealed class UpdateRecipeHandler : IRequestHandler<UpdateRecipeCommand, ErrorOr<Updated>>
{
    private readonly IRecipeRepository _repository;

    public UpdateRecipeHandler(IRecipeRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<Updated>> Handle(UpdateRecipeCommand request, CancellationToken cancellationToken)
    {
        var recipeId = RecipeId.From(request.Id);

        var recipe = await _repository.GetByIdAsync(recipeId, cancellationToken);

        if (recipe is null)
        {
            return Error.NotFound("Recipe.NotFound", $"Recipe '{request.Id}' was not found.");
        }

        recipe.Rename(request.Name);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }
}
