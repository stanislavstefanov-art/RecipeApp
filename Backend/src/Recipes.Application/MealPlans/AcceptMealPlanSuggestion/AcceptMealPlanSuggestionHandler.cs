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
    private readonly IHouseholdRepository _householdRepository;

    public AcceptMealPlanSuggestionHandler(
        IMealPlanRepository mealPlanRepository,
        IRecipeRepository recipeRepository,
        IHouseholdRepository householdRepository)
    {
        _mealPlanRepository = mealPlanRepository;
        _recipeRepository = recipeRepository;
        _householdRepository = householdRepository;
    }

    public async Task<ErrorOr<AcceptMealPlanSuggestionResponse>> Handle(
        AcceptMealPlanSuggestionCommand request,
        CancellationToken cancellationToken)
    {
        var household = await _householdRepository.GetByIdAsync(
            HouseholdId.From(request.HouseholdId),
            cancellationToken);

        if (household is null)
        {
            return Error.NotFound(
                "Household.NotFound",
                $"Household '{request.HouseholdId}' was not found.");
        }

        var householdPersonIds = household.People.Select(x => x.PersonId).ToHashSet();

        var mealPlan = new MealPlan(request.Name, household.Id);

        foreach (var entry in request.Entries)
        {
            if (entry.Assignments.Count == 0)
            {
                return Error.Validation(
                    "MealPlan.InvalidAssignments",
                    "Each meal plan entry must have at least one assignment.");
            }

            var assignedPersonIds = entry.Assignments.Select(x => x.PersonId).ToList();
            if (assignedPersonIds.Count != assignedPersonIds.Distinct().Count())
            {
                return Error.Validation(
                    "MealPlan.InvalidAssignments",
                    "Each person may appear at most once per meal plan entry.");
            }

            var baseRecipe = await _recipeRepository.GetByIdAsync(
                RecipeId.From(entry.BaseRecipeId),
                cancellationToken);

            if (baseRecipe is null)
            {
                return Error.NotFound(
                    "Recipe.NotFound",
                    $"Recipe '{entry.BaseRecipeId}' was not found.");
            }

            foreach (var assignment in entry.Assignments)
            {
                if (!householdPersonIds.Contains(PersonId.From(assignment.PersonId)))
                {
                    return Error.Validation(
                        "MealPlan.InvalidPerson",
                        $"Person '{assignment.PersonId}' does not belong to the specified household.");
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

            RecipeId? saladRecipeId = null;
            if (entry.SaladRecipeId.HasValue)
            {
                var saladRecipe = await _recipeRepository.GetByIdAsync(
                    RecipeId.From(entry.SaladRecipeId.Value),
                    cancellationToken);

                if (saladRecipe is null)
                {
                    return Error.NotFound(
                        "Recipe.NotFound",
                        $"Salad recipe '{entry.SaladRecipeId.Value}' was not found.");
                }

                saladRecipeId = saladRecipe.Id;
            }

            try
            {
                mealPlan.AddRecipe(
                    baseRecipe,
                    entry.PlannedDate,
                    (MealType)entry.MealType,
                    (MealScope)entry.Scope,
                    entry.Assignments
                    .Select(x => (
                        PersonId: PersonId.From(x.PersonId),
                        AssignedRecipeId: RecipeId.From(x.AssignedRecipeId),
                        RecipeVariationId: x.RecipeVariationId.HasValue
                            ? (RecipeVariationId?)RecipeVariationId.From(x.RecipeVariationId.Value)
                            : null,
                        PortionMultiplier: x.PortionMultiplier,
                        Notes: x.Notes))
                    .ToList(),
                    saladRecipeId);
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