using ErrorOr;
using MediatR;
using Recipes.Domain.Enums;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.MealPlans.AddRecipeToMealPlan;

public sealed class AddRecipeToMealPlanHandler
    : IRequestHandler<AddRecipeToMealPlanCommand, ErrorOr<Success>>
{
    private readonly IMealPlanRepository _mealPlanRepository;
    private readonly IRecipeRepository _recipeRepository;

    public AddRecipeToMealPlanHandler(
        IMealPlanRepository mealPlanRepository,
        IRecipeRepository recipeRepository)
    {
        _mealPlanRepository = mealPlanRepository;
        _recipeRepository = recipeRepository;
    }

    public async Task<ErrorOr<Success>> Handle(
        AddRecipeToMealPlanCommand request,
        CancellationToken cancellationToken)
    {
        var mealPlanId = MealPlanId.From(request.MealPlanId);
        var recipeId = RecipeId.From(request.RecipeId);

        var mealPlan = await _mealPlanRepository.GetByIdAsync(mealPlanId, cancellationToken);
        if (mealPlan is null)
        {
            return Error.NotFound(
                "MealPlan.NotFound",
                $"Meal plan '{request.MealPlanId}' was not found.");
        }

        var recipe = await _recipeRepository.GetByIdAsync(recipeId, cancellationToken);
        if (recipe is null)
        {
            return Error.NotFound(
                "Recipe.NotFound",
                $"Recipe '{request.RecipeId}' was not found.");
        }

        try
        {
            mealPlan.AddRecipe(recipe, request.PlannedDate, (MealType)request.MealType);
        }
        catch (InvalidOperationException ex)
        {
            return Error.Conflict("MealPlan.DuplicateEntry", ex.Message);
        }

        await _mealPlanRepository.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}