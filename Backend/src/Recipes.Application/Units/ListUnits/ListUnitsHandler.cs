using ErrorOr;
using MediatR;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Units.ListUnits;

public sealed class ListUnitsHandler : IRequestHandler<ListUnitsQuery, ErrorOr<IReadOnlyList<MeasurementUnitDto>>>
{
    private readonly IMeasurementUnitRepository _repository;

    public ListUnitsHandler(IMeasurementUnitRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<IReadOnlyList<MeasurementUnitDto>>> Handle(
        ListUnitsQuery request,
        CancellationToken cancellationToken)
    {
        var units = await _repository.GetAllAsync(cancellationToken);
        return units.Select(u => new MeasurementUnitDto(u.Id.Value, u.Name, u.Abbreviation)).ToList();
    }
}
