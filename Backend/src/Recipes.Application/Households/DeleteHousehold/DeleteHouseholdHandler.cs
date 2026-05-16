using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Households.DeleteHousehold;

public sealed class DeleteHouseholdHandler : IRequestHandler<DeleteHouseholdCommand, ErrorOr<Deleted>>
{
    private readonly IHouseholdRepository _repository;

    public DeleteHouseholdHandler(IHouseholdRepository repository) => _repository = repository;

    public async Task<ErrorOr<Deleted>> Handle(DeleteHouseholdCommand request, CancellationToken cancellationToken)
    {
        var id = HouseholdId.From(request.Id);
        var entity = await _repository.GetByIdAsync(id, cancellationToken);

        if (entity is null)
            return Error.NotFound("Household.NotFound", $"Household '{request.Id}' was not found.");

        _repository.Remove(entity);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }
}
