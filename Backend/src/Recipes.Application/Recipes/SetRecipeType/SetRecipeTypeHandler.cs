using ErrorOr;
using MediatR;
using Recipes.Domain.Enums;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.SetRecipeType;

public sealed class SetRecipeTypeHandler : IRequestHandler<SetRecipeTypeCommand, ErrorOr<Success>>
{
    private readonly IRecipeRepository _repository;

    public SetRecipeTypeHandler(IRecipeRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<Success>> Handle(
        SetRecipeTypeCommand request,
        CancellationToken cancellationToken)
    {
        var recipeId = RecipeId.From(request.RecipeId);
        var recipe = await _repository.GetByIdAsync(recipeId, cancellationToken);

        if (recipe is null)
            return Error.NotFound("Recipe.NotFound", $"Recipe '{request.RecipeId}' was not found.");

        recipe.SetRecipeType((RecipeType)request.RecipeType);
        await _repository.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
