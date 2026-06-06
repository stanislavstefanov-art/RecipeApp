using ErrorOr;
using MediatR;
using Recipes.Domain.Enums;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.SetRecipeOrigin;

public sealed class SetRecipeOriginHandler : IRequestHandler<SetRecipeOriginCommand, ErrorOr<Success>>
{
    private readonly IRecipeRepository _repository;

    public SetRecipeOriginHandler(IRecipeRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<Success>> Handle(
        SetRecipeOriginCommand request,
        CancellationToken cancellationToken)
    {
        var recipeId = RecipeId.From(request.RecipeId);
        var recipe = await _repository.GetByIdAsync(recipeId, cancellationToken);

        if (recipe is null)
            return Error.NotFound("Recipe.NotFound", $"Recipe '{request.RecipeId}' was not found.");

        recipe.SetOrigin((RecipeOrigin)request.Origin);
        await _repository.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
