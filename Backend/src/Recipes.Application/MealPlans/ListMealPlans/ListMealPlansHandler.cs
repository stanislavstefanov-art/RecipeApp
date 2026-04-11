using ErrorOr;
using MediatR;
using Recipes.Domain.Repositories;

namespace Recipes.Application.MealPlans.ListMealPlans;

public sealed class ListMealPlansHandler
    : IRequestHandler<ListMealPlansQuery, ErrorOr<IReadOnlyList<MealPlanListItemDto>>>
{
    private readonly IMealPlanRepository _mealPlanRepository;

    public ListMealPlansHandler(IMealPlanRepository mealPlanRepository)
    {
        _mealPlanRepository = mealPlanRepository;
    }

    public async Task<ErrorOr<IReadOnlyList<MealPlanListItemDto>>> Handle(
        ListMealPlansQuery request,
        CancellationToken cancellationToken)
    {
        var mealPlans = await _mealPlanRepository.GetAllAsync(cancellationToken);

        var result = mealPlans
            .Select(x => new MealPlanListItemDto(
                x.Id.Value,
                x.Name,
                x.Entries.Count))
            .ToList();

        return result;
    }
}