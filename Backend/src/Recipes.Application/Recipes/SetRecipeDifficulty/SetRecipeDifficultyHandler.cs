using ErrorOr;
using MediatR;
using Recipes.Domain.Enums;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.SetRecipeDifficulty;

public sealed class SetRecipeDifficultyHandler : IRequestHandler<SetRecipeDifficultyCommand, ErrorOr<Success>>
{
    private readonly IRecipeRepository _repository;

    public SetRecipeDifficultyHandler(IRecipeRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<Success>> Handle(
        SetRecipeDifficultyCommand request,
        CancellationToken cancellationToken)
    {
        var recipeId = RecipeId.From(request.RecipeId);
        var recipe = await _repository.GetByIdAsync(recipeId, cancellationToken);

        if (recipe is null)
            return Error.NotFound("Recipe.NotFound", $"Recipe '{request.RecipeId}' was not found.");

        var level = request.DifficultyLevel.HasValue
            ? (DifficultyLevel?)request.DifficultyLevel.Value
            : null;

        recipe.SetDifficulty(level);
        await _repository.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
