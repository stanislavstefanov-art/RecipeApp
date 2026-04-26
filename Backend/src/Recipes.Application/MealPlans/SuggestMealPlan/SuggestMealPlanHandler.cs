using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.MealPlans.SuggestMealPlan;

public sealed class SuggestMealPlanHandler
    : IRequestHandler<SuggestMealPlanCommand, ErrorOr<MealPlanSuggestionDto>>
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly IPersonRepository _personRepository;
    private readonly IMealPlanSuggestionService _mealPlanSuggestionService;

    public SuggestMealPlanHandler(
        IRecipeRepository recipeRepository,
        IHouseholdRepository householdRepository,
        IPersonRepository personRepository,
        IMealPlanSuggestionService mealPlanSuggestionService)
    {
        _recipeRepository = recipeRepository;
        _householdRepository = householdRepository;
        _personRepository = personRepository;
        _mealPlanSuggestionService = mealPlanSuggestionService;
    }

    public async Task<ErrorOr<MealPlanSuggestionDto>> Handle(
        SuggestMealPlanCommand request,
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

        var memberIds = household.Members.Select(x => x.PersonId).ToList();
        if (memberIds.Count == 0)
        {
            return Error.Validation(
                "MealPlanSuggestion.NoHouseholdMembers",
                "The selected household has no members.");
        }

        var persons = await _personRepository.GetByIdsAsync(memberIds, cancellationToken);

        var recipes = await _recipeRepository.GetAllAsync(cancellationToken);
        if (recipes.Count == 0)
        {
            return Error.Validation(
                "MealPlanSuggestion.NoRecipes",
                "At least one recipe is required to suggest a meal plan.");
        }

        var suggestionRequest = new MealPlanSuggestionRequestDto(
            request.Name,
            request.StartDate,
            request.NumberOfDays,
            request.MealTypes,
            new HouseholdPlanningProfileDto(
                household.Id.Value,
                household.Name,
                persons.Select(x => new PersonPlanningProfileDto(
                    x.Id.Value,
                    x.Name,
                    x.DietaryPreferences.Select(y => (int)y).ToList(),
                    x.HealthConcerns.Select(y => (int)y).ToList(),
                    x.Notes)).ToList()),
            recipes.Select(x => new AvailableRecipeDto(
                x.Id.Value,
                x.Name.Value,
                x.Variations.Select(v => new AvailableRecipeVariationDto(
                    v.Id.Value,
                    v.Name,
                    v.Notes,
                    v.IngredientAdjustmentNotes)).ToList()))
            .ToList());

        var suggestion = await _mealPlanSuggestionService.SuggestAsync(
            suggestionRequest,
            cancellationToken);

        return suggestion;
    }
}