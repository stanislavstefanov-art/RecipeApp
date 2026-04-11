using ErrorOr;
using MediatR;
using Recipes.Domain.Entities;
using Recipes.Domain.Repositories;

namespace Recipes.Application.MealPlans.CreateMealPlan;

public sealed class CreateMealPlanHandler
    : IRequestHandler<CreateMealPlanCommand, ErrorOr<CreateMealPlanResponse>>
{
    private readonly IMealPlanRepository _mealPlanRepository;

    public CreateMealPlanHandler(IMealPlanRepository mealPlanRepository)
    {
        _mealPlanRepository = mealPlanRepository;
    }

    public async Task<ErrorOr<CreateMealPlanResponse>> Handle(
        CreateMealPlanCommand request,
        CancellationToken cancellationToken)
    {
        var mealPlan = new MealPlan(request.Name);

        await _mealPlanRepository.AddAsync(mealPlan, cancellationToken);
        await _mealPlanRepository.SaveChangesAsync(cancellationToken);

        return new CreateMealPlanResponse(mealPlan.Id.Value, mealPlan.Name);
    }
}