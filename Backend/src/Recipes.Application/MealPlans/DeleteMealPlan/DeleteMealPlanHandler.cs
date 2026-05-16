using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.MealPlans.DeleteMealPlan;

public sealed class DeleteMealPlanHandler : IRequestHandler<DeleteMealPlanCommand, ErrorOr<Deleted>>
{
    private readonly IMealPlanRepository _repository;

    public DeleteMealPlanHandler(IMealPlanRepository repository) => _repository = repository;

    public async Task<ErrorOr<Deleted>> Handle(DeleteMealPlanCommand request, CancellationToken cancellationToken)
    {
        var id = MealPlanId.From(request.Id);
        var entity = await _repository.GetByIdAsync(id, cancellationToken);

        if (entity is null)
            return Error.NotFound("MealPlan.NotFound", $"Meal plan '{request.Id}' was not found.");

        _repository.Remove(entity);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }
}
