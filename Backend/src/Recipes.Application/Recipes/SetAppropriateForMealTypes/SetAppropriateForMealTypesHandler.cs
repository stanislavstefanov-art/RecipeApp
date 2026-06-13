using ErrorOr;
using MediatR;
using Recipes.Domain.Enums;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.SetAppropriateForMealTypes;

public sealed class SetAppropriateForMealTypesHandler : IRequestHandler<SetAppropriateForMealTypesCommand, ErrorOr<Success>>
{
    private readonly IRecipeRepository _repository;

    public SetAppropriateForMealTypesHandler(IRecipeRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<Success>> Handle(SetAppropriateForMealTypesCommand request, CancellationToken cancellationToken)
    {
        var recipe = await _repository.GetByIdAsync(RecipeId.From(request.RecipeId), cancellationToken);
        if (recipe is null)
            return Error.NotFound("Recipe.NotFound", $"Recipe '{request.RecipeId}' was not found.");

        recipe.SetAppropriateForMealTypes(request.MealTypes.Select(m => (MealType)m).ToList());
        await _repository.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
