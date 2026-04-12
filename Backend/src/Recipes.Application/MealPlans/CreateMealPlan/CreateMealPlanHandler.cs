using ErrorOr;
using MediatR;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.MealPlans.CreateMealPlan;

public sealed class CreateMealPlanHandler
    : IRequestHandler<CreateMealPlanCommand, ErrorOr<CreateMealPlanResponse>>
{
    private readonly IMealPlanRepository _mealPlanRepository;
    private readonly IHouseholdRepository _householdRepository;

    public CreateMealPlanHandler(
        IMealPlanRepository mealPlanRepository,
        IHouseholdRepository householdRepository)
    {
        _mealPlanRepository = mealPlanRepository;
        _householdRepository = householdRepository;
    }

    public async Task<ErrorOr<CreateMealPlanResponse>> Handle(
        CreateMealPlanCommand request,
        CancellationToken cancellationToken)
    {
        var household = await _householdRepository.GetByIdAsync(HouseholdId.From(request.HouseholdId), cancellationToken);
        if (household is null)
        {
            return Error.NotFound("Household.NotFound", $"Household '{request.HouseholdId}' was not found.");
        }

        var mealPlan = new MealPlan(request.Name, household.Id);

        await _mealPlanRepository.AddAsync(mealPlan, cancellationToken);
        await _mealPlanRepository.SaveChangesAsync(cancellationToken);

        return new CreateMealPlanResponse(mealPlan.Id.Value, mealPlan.Name);
    }
}