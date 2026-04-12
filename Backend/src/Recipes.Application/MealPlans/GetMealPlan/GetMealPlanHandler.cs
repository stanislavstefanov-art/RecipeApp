using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.MealPlans.GetMealPlan;

public sealed class GetMealPlanHandler
    : IRequestHandler<GetMealPlanQuery, ErrorOr<MealPlanDetailsDto>>
{
    private readonly IMealPlanRepository _mealPlanRepository;
    private readonly IRecipeRepository _recipeRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly IPersonRepository _personRepository;

    public GetMealPlanHandler(
        IMealPlanRepository mealPlanRepository,
        IRecipeRepository recipeRepository,
        IHouseholdRepository householdRepository,
        IPersonRepository personRepository)
    {
        _mealPlanRepository = mealPlanRepository;
        _recipeRepository = recipeRepository;
        _householdRepository = householdRepository;
        _personRepository = personRepository;
    }

    public async Task<ErrorOr<MealPlanDetailsDto>> Handle(
        GetMealPlanQuery request,
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

        var recipes = await _recipeRepository.GetAllAsync(cancellationToken);
        var recipeById = recipes.ToDictionary(x => x.Id, x => x);

        var persons = await _personRepository.GetAllAsync(cancellationToken);
        var personById = persons.ToDictionary(x => x.Id, x => x);

        var entries = mealPlan.Entries
            .OrderBy(x => x.PlannedDate)
            .ThenBy(x => x.MealType)
            .Select(entry =>
            {
                recipeById.TryGetValue(entry.BaseRecipeId, out var baseRecipe);

                var assignments = entry.PersonAssignments
                    .Select(a =>
                    {
                        recipeById.TryGetValue(a.AssignedRecipeId, out var assignedRecipe);
                        personById.TryGetValue(a.PersonId, out var person);

                        var variationName = a.RecipeVariationId.HasValue && assignedRecipe is not null
                            ? assignedRecipe.Variations
                                .SingleOrDefault(v => v.Id == a.RecipeVariationId.Value)?
                                .Name
                            : null;

                        return new MealPlanEntryAssignmentDto(
                            a.PersonId.Value,
                            person?.Name ?? a.PersonId.Value.ToString(),
                            a.AssignedRecipeId.Value,
                            assignedRecipe?.Name.Value ?? a.AssignedRecipeId.Value.ToString(),
                            a.RecipeVariationId?.Value,
                            variationName,
                            a.PortionMultiplier,
                            a.Notes);
                    })
                    .OrderBy(x => x.PersonName)
                    .ToList();

                return new MealPlanEntryDto(
                    entry.Id.Value,
                    entry.BaseRecipeId.Value,
                    baseRecipe?.Name.Value ?? entry.BaseRecipeId.Value.ToString(),
                    entry.PlannedDate,
                    (int)entry.MealType,
                    (int)entry.Scope,
                    assignments);
            })
            .ToList();

        return new MealPlanDetailsDto(
            mealPlan.Id.Value,
            mealPlan.Name,
            household.Id.Value,
            household.Name,
            entries);
    }
}