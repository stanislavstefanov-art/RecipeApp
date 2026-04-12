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
    private readonly IHouseholdRepository _householdRepository;

    public AddRecipeToMealPlanHandler(
        IMealPlanRepository mealPlanRepository,
        IRecipeRepository recipeRepository,
        IHouseholdRepository householdRepository)
    {
        _mealPlanRepository = mealPlanRepository;
        _recipeRepository = recipeRepository;
        _householdRepository = householdRepository;
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

        var household = await _householdRepository.GetByIdAsync(mealPlan.HouseholdId, cancellationToken);
        if (household is null)
        {
            return Error.NotFound(
                "Household.NotFound",
                $"Household '{mealPlan.HouseholdId.Value}' was not found.");
        }

        var baseRecipe = await _recipeRepository.GetByIdAsync(recipeId, cancellationToken);
        if (baseRecipe is null)
        {
            return Error.NotFound(
                "Recipe.NotFound",
                $"Recipe '{request.RecipeId}' was not found.");
        }

        var householdPersonIds = household.Members.Select(x => x.PersonId).ToHashSet();

        foreach (var assignment in request.Assignments)
        {
            var personId = PersonId.From(assignment.PersonId);

            if (!householdPersonIds.Contains(personId))
            {
                return Error.Validation(
                    "MealPlan.InvalidPerson",
                    $"Person '{assignment.PersonId}' does not belong to the meal plan household.");
            }

            var assignedRecipe = await _recipeRepository.GetByIdAsync(
                RecipeId.From(assignment.AssignedRecipeId),
                cancellationToken);

            if (assignedRecipe is null)
            {
                return Error.NotFound(
                    "Recipe.NotFound",
                    $"Assigned recipe '{assignment.AssignedRecipeId}' was not found.");
            }

            if (assignment.RecipeVariationId.HasValue)
            {
                var variationId = RecipeVariationId.From(assignment.RecipeVariationId.Value);

                var variationExists = assignedRecipe.Variations.Any(v => v.Id == variationId);
                if (!variationExists)
                {
                    return Error.Validation(
                        "MealPlan.InvalidVariation",
                        $"Variation '{assignment.RecipeVariationId}' does not belong to recipe '{assignment.AssignedRecipeId}'.");
                }
            }
        }

        try
        {
            mealPlan.AddRecipe(
            baseRecipe,
            request.PlannedDate,
            (MealType)request.MealType,
            (MealScope)request.Scope,
            request.Assignments
            .Select(x => (
                PersonId: PersonId.From(x.PersonId),
                AssignedRecipeId: RecipeId.From(x.AssignedRecipeId),
                RecipeVariationId: x.RecipeVariationId.HasValue
                    ? (RecipeVariationId?)RecipeVariationId.From(x.RecipeVariationId.Value)
                    : null,
                PortionMultiplier: x.PortionMultiplier,
                Notes: x.Notes))
            .ToList());
        }
        catch (InvalidOperationException ex)
        {
            return Error.Conflict("MealPlan.DuplicateEntry", ex.Message);
        }

        await _mealPlanRepository.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}