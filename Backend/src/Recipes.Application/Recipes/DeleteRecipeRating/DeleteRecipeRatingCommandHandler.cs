using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.DeleteRecipeRating;

public sealed class DeleteRecipeRatingCommandHandler : IRequestHandler<DeleteRecipeRatingCommand, ErrorOr<Deleted>>
{
    private readonly IRecipeRepository _repository;
    private readonly ICurrentUser _currentUser;

    public DeleteRecipeRatingCommandHandler(IRecipeRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<Deleted>> Handle(DeleteRecipeRatingCommand request, CancellationToken cancellationToken)
    {
        var recipeId = RecipeId.From(request.RecipeId);
        var recipe = await _repository.GetByIdAsync(recipeId, cancellationToken);

        if (recipe is null)
            return Error.NotFound("Recipe.NotFound", $"Recipe '{request.RecipeId}' was not found.");

        if (recipe.HouseholdId.HasValue)
        {
            var memberIds = await _currentUser.GetHouseholdIdsAsync(cancellationToken);
            if (!memberIds.Contains(recipe.HouseholdId.Value))
                return Error.NotFound("Recipe.NotFound", $"Recipe '{request.RecipeId}' was not found.");
        }

        var removed = recipe.RemoveRating(_currentUser.UserId);
        if (!removed)
            return Error.NotFound("RecipeRating.NotFound", "You have not rated this recipe.");

        await _repository.SaveChangesAsync(cancellationToken);
        return Result.Deleted;
    }
}
