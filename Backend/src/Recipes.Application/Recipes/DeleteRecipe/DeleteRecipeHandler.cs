using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.DeleteRecipe;

public sealed class DeleteRecipeHandler : IRequestHandler<DeleteRecipeCommand, ErrorOr<Deleted>>
{
    private readonly IRecipeRepository _repository;

    public DeleteRecipeHandler(IRecipeRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<Deleted>> Handle(DeleteRecipeCommand request, CancellationToken cancellationToken)
    {
        var recipeId = RecipeId.From(request.Id);

        var recipe = await _repository.GetByIdAsync(recipeId, cancellationToken);

        if (recipe is null)
        {
            return Error.NotFound("Recipe.NotFound", $"Recipe '{request.Id}' was not found.");
        }

        _repository.Remove(recipe);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }
}
