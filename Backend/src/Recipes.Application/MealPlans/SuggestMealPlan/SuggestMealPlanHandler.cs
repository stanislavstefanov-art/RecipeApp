using ErrorOr;
using MediatR;
using Recipes.Domain.Repositories;

namespace Recipes.Application.MealPlans.SuggestMealPlan;

public sealed class SuggestMealPlanHandler
    : IRequestHandler<SuggestMealPlanCommand, ErrorOr<MealPlanSuggestionDto>>
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly IMealPlanSuggestionService _mealPlanSuggestionService;

    public SuggestMealPlanHandler(
        IRecipeRepository recipeRepository,
        IMealPlanSuggestionService mealPlanSuggestionService)
    {
        _recipeRepository = recipeRepository;
        _mealPlanSuggestionService = mealPlanSuggestionService;
    }

    public async Task<ErrorOr<MealPlanSuggestionDto>> Handle(
        SuggestMealPlanCommand request,
        CancellationToken cancellationToken)
    {
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
            recipes.Select(x => new AvailableRecipeDto(x.Id.Value, x.Name.Value))
                .ToList());

        var suggestion = await _mealPlanSuggestionService.SuggestAsync(
            suggestionRequest,
            cancellationToken);

        return suggestion;
    }
}