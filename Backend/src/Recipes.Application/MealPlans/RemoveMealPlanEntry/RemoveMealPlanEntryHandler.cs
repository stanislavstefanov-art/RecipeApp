using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.MealPlans.RemoveMealPlanEntry;

public sealed class RemoveMealPlanEntryHandler : IRequestHandler<RemoveMealPlanEntryCommand, ErrorOr<Success>>
{
    private readonly IMealPlanRepository _repository;

    public RemoveMealPlanEntryHandler(IMealPlanRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<Success>> Handle(RemoveMealPlanEntryCommand request, CancellationToken cancellationToken)
    {
        var mealPlan = await _repository.GetByIdAsync(MealPlanId.From(request.MealPlanId), cancellationToken);
        if (mealPlan is null)
            return Error.NotFound("MealPlan.NotFound", $"Meal plan '{request.MealPlanId}' was not found.");

        mealPlan.RemoveEntry(MealPlanEntryId.From(request.EntryId));
        await _repository.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
