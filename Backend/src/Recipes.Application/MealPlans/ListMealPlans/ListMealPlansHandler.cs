using ErrorOr;
using MediatR;
using Recipes.Domain.Repositories;

namespace Recipes.Application.MealPlans.ListMealPlans;

public sealed class ListMealPlansHandler
    : IRequestHandler<ListMealPlansQuery, ErrorOr<IReadOnlyList<MealPlanListItemDto>>>
{
    private readonly IMealPlanRepository _mealPlanRepository;
    private readonly IHouseholdRepository _householdRepository;

    public ListMealPlansHandler(
        IMealPlanRepository mealPlanRepository,
        IHouseholdRepository householdRepository)
    {
        _mealPlanRepository = mealPlanRepository;
        _householdRepository = householdRepository;
    }

    public async Task<ErrorOr<IReadOnlyList<MealPlanListItemDto>>> Handle(
        ListMealPlansQuery request,
        CancellationToken cancellationToken)
    {
        var mealPlans = await _mealPlanRepository.GetAllAsync(cancellationToken);
        var households = await _householdRepository.GetAllAsync(cancellationToken);
        var householdsById = households.ToDictionary(x => x.Id, x => x.Name);

        var result = mealPlans
            .Select(x => new MealPlanListItemDto(
                x.Id.Value,
                x.Name,
                x.HouseholdId.Value,
                householdsById.TryGetValue(x.HouseholdId, out var householdName)
                    ? householdName
                    : x.HouseholdId.Value.ToString(),
                x.Entries.Count))
            .ToList();

        return result;
    }
}