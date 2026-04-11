using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.MealPlans.GetMealPlan;

public sealed class GetMealPlanHandler
    : IRequestHandler<GetMealPlanQuery, ErrorOr<MealPlanDetailsDto>>
{
    private readonly IMealPlanRepository _mealPlanRepository;

    public GetMealPlanHandler(IMealPlanRepository mealPlanRepository)
    {
        _mealPlanRepository = mealPlanRepository;
    }

    public async Task<ErrorOr<MealPlanDetailsDto>> Handle(
        GetMealPlanQuery request,
        CancellationToken cancellationToken)
    {
        var id = MealPlanId.From(request.MealPlanId);

        var mealPlan = await _mealPlanRepository.GetByIdAsync(id, cancellationToken);
        if (mealPlan is null)
        {
            return Error.NotFound(
                "MealPlan.NotFound",
                $"Meal plan '{request.MealPlanId}' was not found.");
        }

        return new MealPlanDetailsDto(
            mealPlan.Id.Value,
            mealPlan.Name,
            mealPlan.Entries
                .OrderBy(x => x.PlannedDate)
                .ThenBy(x => x.MealType)
                .Select(x => new MealPlanEntryDto(
                    x.Id.Value,
                    x.RecipeId.Value,
                    x.PlannedDate,
                    (int)x.MealType))
                .ToList());
    }
}