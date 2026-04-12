using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.MealPlans.UpdateMealPlanPersonAssignment;

public sealed class UpdateMealPlanPersonAssignmentHandler
    : IRequestHandler<UpdateMealPlanPersonAssignmentCommand, ErrorOr<Success>>
{
    private readonly IMealPlanRepository _mealPlanRepository;
    private readonly IRecipeRepository _recipeRepository;
    private readonly IHouseholdRepository _householdRepository;

    public UpdateMealPlanPersonAssignmentHandler(
        IMealPlanRepository mealPlanRepository,
        IRecipeRepository recipeRepository,
        IHouseholdRepository householdRepository)
    {
        _mealPlanRepository = mealPlanRepository;
        _recipeRepository = recipeRepository;
        _householdRepository = householdRepository;
    }

    public async Task<ErrorOr<Success>> Handle(
        UpdateMealPlanPersonAssignmentCommand request,
        CancellationToken cancellationToken)
    {
        var mealPlan = await _mealPlanRepository.GetByIdAsync(
            MealPlanId.From(request.MealPlanId),
            cancellationToken);

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

        var personId = PersonId.From(request.PersonId);

        var personBelongsToHousehold = household.Members.Any(x => x.PersonId == personId);
        if (!personBelongsToHousehold)
        {
            return Error.Validation(
                "MealPlan.InvalidPerson",
                $"Person '{request.PersonId}' does not belong to the meal plan household.");
        }

        var assignedRecipe = await _recipeRepository.GetByIdAsync(
            RecipeId.From(request.AssignedRecipeId),
            cancellationToken);

        if (assignedRecipe is null)
        {
            return Error.NotFound(
                "Recipe.NotFound",
                $"Recipe '{request.AssignedRecipeId}' was not found.");
        }

        RecipeVariationId? variationId = null;

        if (request.RecipeVariationId.HasValue)
        {
            variationId = RecipeVariationId.From(request.RecipeVariationId.Value);

            var variationExists = assignedRecipe.Variations.Any(v => v.Id == variationId.Value);
            if (!variationExists)
            {
                return Error.Validation(
                    "MealPlan.InvalidVariation",
                    $"Variation '{request.RecipeVariationId}' does not belong to recipe '{request.AssignedRecipeId}'.");
            }
        }

        try
        {
            mealPlan.UpdatePersonAssignment(
                MealPlanEntryId.From(request.MealPlanEntryId),
                personId,
                assignedRecipe.Id,
                variationId,
                request.PortionMultiplier,
                request.Notes);
        }
        catch (InvalidOperationException ex)
        {
            return Error.NotFound(
                "MealPlan.AssignmentNotFound",
                ex.Message);
        }

        await _mealPlanRepository.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}