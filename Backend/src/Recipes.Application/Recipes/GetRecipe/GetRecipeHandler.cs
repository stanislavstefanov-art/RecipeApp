using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Application.Recipes;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.GetRecipe;

public sealed class GetRecipeHandler : IRequestHandler<GetRecipeQuery, ErrorOr<RecipeDto>>
{
    private readonly IRecipeRepository _repository;
    private readonly ICurrentUser _currentUser;

    public GetRecipeHandler(IRecipeRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<RecipeDto>> Handle(GetRecipeQuery request, CancellationToken cancellationToken)
    {
        var recipeId = RecipeId.From(request.Id);
        var recipe = await _repository.GetByIdAsync(recipeId, cancellationToken);

        if (recipe is null)
        {
            return Error.NotFound("Recipe.NotFound", $"Recipe '{request.Id}' was not found.");
        }

        if (recipe.HouseholdId.HasValue)
        {
            var memberIds = await _currentUser.GetHouseholdIdsAsync(cancellationToken);
            if (!memberIds.Contains(recipe.HouseholdId.Value))
            {
                return Error.NotFound("Recipe.NotFound", $"Recipe '{request.Id}' was not found.");
            }
        }

        var myUserId = _currentUser.UserId;
        var ratingDtos = recipe.Ratings
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RecipeRatingDto(r.Id.Value, r.UserId.Value, r.Stars, r.Comment, r.CreatedAt, r.UpdatedAt))
            .ToList();
        var myRating = ratingDtos.FirstOrDefault(r => r.UserId == myUserId.Value);

        return new RecipeDto(
            recipe.Id.Value,
            recipe.Name.Value,
            recipe.Ingredients.Select(i => new IngredientDto(i.Id.Value, i.Name, i.Quantity, i.Unit)).ToList(),
            recipe.Steps.OrderBy(s => s.Order).Select(s => new RecipeStepDto(s.Id.Value, s.Order, s.Instruction)).ToList(),
            recipe.AverageStars,
            recipe.RatingCount,
            ratingDtos,
            myRating,
            recipe.ImageUrl);
    }
}
