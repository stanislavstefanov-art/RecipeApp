using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Units.DeleteUnit;

public sealed class DeleteUnitHandler : IRequestHandler<DeleteUnitCommand, ErrorOr<Deleted>>
{
    private readonly IMeasurementUnitRepository _repository;

    public DeleteUnitHandler(IMeasurementUnitRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<Deleted>> Handle(DeleteUnitCommand request, CancellationToken cancellationToken)
    {
        var unit = await _repository.GetByIdAsync(MeasurementUnitId.From(request.Id), cancellationToken);
        if (unit is null)
            return Error.NotFound("Unit.NotFound", "Unit not found.");

        _repository.Remove(unit);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }
}
