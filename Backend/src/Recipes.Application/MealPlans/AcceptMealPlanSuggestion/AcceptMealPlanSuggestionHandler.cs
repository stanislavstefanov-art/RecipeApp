using ErrorOr;
using MediatR;
using Recipes.Domain.Enums;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.MealPlans.AcceptMealPlanSuggestion;

public sealed class AcceptMealPlanSuggestionHandler
    : IRequestHandler<AcceptMealPlanSuggestionCommand, ErrorOr<AcceptMealPlanSuggestionResponse>>
{
    private readonly IMealPlanRepository _mealPlanRepository;
    private readonly IRecipeRepository _recipeRepository;

    public AcceptMealPlanSuggestionHandler(
        IMealPlanRepository mealPlanRepository,
        IRecipeRepository recipeRepository)
    {
        _mealPlanRepository = mealPlanRepository;
        _recipeRepository = recipeRepository;
    }

    public async Task<ErrorOr<AcceptMealPlanSuggestionResponse>> Handle(
        AcceptMealPlanSuggestionCommand request,
        CancellationToken cancellationToken)
    {
        var mealPlan = new MealPlan(request.Name);

        foreach (var entry in request.Entries)
        {
            var recipeId = RecipeId.From(entry.RecipeId);

            var recipe = await _recipeRepository.GetByIdAsync(recipeId, cancellationToken);
            if (recipe is null)
            {
                return Error.NotFound(
                    "Recipe.NotFound",
                    $"Recipe '{entry.RecipeId}' was not found.");
            }

            try
            {
                mealPlan.AddRecipe(recipe, entry.PlannedDate, (MealType)entry.MealType);
            }
            catch (InvalidOperationException ex)
            {
                return Error.Conflict("MealPlan.InvalidSuggestion", ex.Message);
            }
        }

        await _mealPlanRepository.AddAsync(mealPlan, cancellationToken);
        await _mealPlanRepository.SaveChangesAsync(cancellationToken);

        return new AcceptMealPlanSuggestionResponse(mealPlan.Id.Value, mealPlan.Name);
    }
}