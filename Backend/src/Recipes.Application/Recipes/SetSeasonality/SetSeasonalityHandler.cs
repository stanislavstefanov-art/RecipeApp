using ErrorOr;
using MediatR;
using Recipes.Domain.Enums;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.SetSeasonality;

public sealed class SetSeasonalityHandler : IRequestHandler<SetSeasonalityCommand, ErrorOr<Success>>
{
    private readonly IRecipeRepository _repository;

    public SetSeasonalityHandler(IRecipeRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<Success>> Handle(SetSeasonalityCommand request, CancellationToken cancellationToken)
    {
        var recipe = await _repository.GetByIdAsync(RecipeId.From(request.RecipeId), cancellationToken);
        if (recipe is null)
            return Error.NotFound("Recipe.NotFound", $"Recipe '{request.RecipeId}' was not found.");

        recipe.SetSeasonality((Season)request.Seasonality);
        await _repository.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
