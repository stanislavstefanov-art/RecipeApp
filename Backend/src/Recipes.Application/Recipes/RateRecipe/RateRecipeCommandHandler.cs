using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.RateRecipe;

public sealed class RateRecipeCommandHandler : IRequestHandler<RateRecipeCommand, ErrorOr<RecipeRatingDto>>
{
    private readonly IRecipeRepository _repository;
    private readonly ICurrentUser _currentUser;

    public RateRecipeCommandHandler(IRecipeRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<RecipeRatingDto>> Handle(RateRecipeCommand request, CancellationToken cancellationToken)
    {
        var recipeId = RecipeId.From(request.RecipeId);
        var recipe = await _repository.GetByIdAsync(recipeId, cancellationToken);

        if (recipe is null)
            return Error.NotFound("Recipe.NotFound", $"Recipe '{request.RecipeId}' was not found.");

        if (recipe.HouseholdId.HasValue)
        {
            var memberIds = await _currentUser.GetHouseholdIdsAsync(cancellationToken);
            if (!memberIds.Contains(recipe.HouseholdId.Value))
                return Error.Forbidden("Recipe.NotFound", $"Recipe '{request.RecipeId}' was not found.");
        }

        var rating = recipe.Rate(_currentUser.UserId, request.Stars, request.Comment, DateTimeOffset.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);

        return new RecipeRatingDto(rating.Id.Value, rating.UserId.Value, rating.Stars, rating.Comment, rating.CreatedAt, rating.UpdatedAt);
    }
}
