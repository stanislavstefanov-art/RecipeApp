using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.SetMealsPerCook;

public sealed class SetMealsPerCookHandler : IRequestHandler<SetMealsPerCookCommand, ErrorOr<Success>>
{
    private readonly IRecipeRepository _repository;

    public SetMealsPerCookHandler(IRecipeRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<Success>> Handle(SetMealsPerCookCommand request, CancellationToken cancellationToken)
    {
        var recipe = await _repository.GetByIdAsync(RecipeId.From(request.RecipeId), cancellationToken);
        if (recipe is null)
            return Error.NotFound("Recipe.NotFound", $"Recipe '{request.RecipeId}' was not found.");

        recipe.SetMealsPerCook(request.MealsPerCook);
        await _repository.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
