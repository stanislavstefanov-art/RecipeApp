using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Enums;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.MealPlans.SuggestMealPlan;

public sealed class SuggestMealPlanHandler
    : IRequestHandler<SuggestMealPlanCommand, ErrorOr<MealPlanSuggestionDto>>
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly IPersonRepository _personRepository;
    private readonly ICookingLogRepository _cookingLogRepository;
    private readonly IMealPlanSuggestionService _mealPlanSuggestionService;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _time;

    public SuggestMealPlanHandler(
        IRecipeRepository recipeRepository,
        IHouseholdRepository householdRepository,
        IPersonRepository personRepository,
        ICookingLogRepository cookingLogRepository,
        IMealPlanSuggestionService mealPlanSuggestionService,
        ICurrentUser currentUser,
        TimeProvider time)
    {
        _recipeRepository = recipeRepository;
        _householdRepository = householdRepository;
        _personRepository = personRepository;
        _cookingLogRepository = cookingLogRepository;
        _mealPlanSuggestionService = mealPlanSuggestionService;
        _currentUser = currentUser;
        _time = time;
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

        var memberIds = household.People.Select(x => x.PersonId).ToList();
        if (memberIds.Count == 0)
        {
            return Error.Validation(
                "MealPlanSuggestion.NoHouseholdMembers",
                "The selected household has no members.");
        }

        var persons = await _personRepository.GetByIdsAsync(memberIds, cancellationToken);

        var allRecipes = await _recipeRepository.GetAllAsync(cancellationToken);
        var recipes = request.RecipeSource switch
        {
            "manual" => allRecipes.Where(r => !r.IsImported).ToList(),
            "imported" => allRecipes.Where(r => r.IsImported).ToList(),
            _ => allRecipes,
        };
        recipes = request.RecipeOrigin switch
        {
            "home" => recipes.Where(r => r.Origin == RecipeOrigin.Home).ToList(),
            "borrowed" => recipes.Where(r => r.Origin == RecipeOrigin.Borrowed).ToList(),
            _ => recipes.Where(r => r.Origin != RecipeOrigin.Bought).ToList(),
        };
        if (recipes.Count == 0)
        {
            return Error.Validation(
                "MealPlanSuggestion.NoRecipes",
                "At least one recipe is required to suggest a meal plan.");
        }

        var recipeNameMap = recipes.ToDictionary(r => r.Id, r => r.Name.Value);
        var today = DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime);

        var cookingLog = await _cookingLogRepository.GetAllByUserAsync(_currentUser.UserId, cancellationToken);
        var recentlyCookedRecipes = cookingLog
            .GroupBy(e => e.RecipeId)
            .Select(g => new { RecipeId = g.Key, LastCookedOn = g.Max(e => e.CookedOn) })
            .Where(x => recipeNameMap.ContainsKey(x.RecipeId))
            .Select(x => new RecentlyCookedDto(
                x.RecipeId.Value,
                recipeNameMap[x.RecipeId],
                today.DayNumber - x.LastCookedOn.DayNumber))
            .OrderBy(x => x.DaysAgo)
            .ToList();

        var availableIngredients = request.PriorityIngredients is { Count: > 0 }
            ? request.PriorityIngredients
            : null;

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
                (int)x.RecipeType,
                x.MealsPerCook,
                x.AppropriateForMealTypes.Select(m => (int)m).ToList(),
                x.Variations.Select(v => new AvailableRecipeVariationDto(
                    v.Id.Value,
                    v.Name,
                    v.Notes,
                    v.IngredientAdjustmentNotes)).ToList(),
                (int)x.Seasonality))
            .ToList(),
            request.PersonsPerMealType,
            availableIngredients,
            recentlyCookedRecipes.Count > 0 ? recentlyCookedRecipes : null,
            CurrentSeason: GetSeasonName(request.StartDate));

        var suggestion = await _mealPlanSuggestionService.SuggestAsync(
            suggestionRequest,
            cancellationToken);

        return suggestion;
    }

    private static string GetSeasonName(DateOnly date) => date.Month switch
    {
        3 or 4 or 5 => "Spring",
        6 or 7 or 8 => "Summer",
        9 or 10 or 11 => "Autumn",
        _ => "Winter",
    };
}
