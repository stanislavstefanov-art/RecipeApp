using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.MealPlans.SwapMealPlanEntries;

public sealed class SwapMealPlanEntriesHandler : IRequestHandler<SwapMealPlanEntriesCommand, ErrorOr<Success>>
{
    private readonly IMealPlanRepository _repository;

    public SwapMealPlanEntriesHandler(IMealPlanRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<Success>> Handle(SwapMealPlanEntriesCommand request, CancellationToken cancellationToken)
    {
        var mealPlan = await _repository.GetByIdAsync(MealPlanId.From(request.MealPlanId), cancellationToken);
        if (mealPlan is null)
            return Error.NotFound("MealPlan.NotFound", $"Meal plan '{request.MealPlanId}' was not found.");

        mealPlan.SwapEntries(MealPlanEntryId.From(request.EntryAId), MealPlanEntryId.From(request.EntryBId));
        await _repository.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
