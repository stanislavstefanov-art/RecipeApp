using ErrorOr;
using MediatR;
using Recipes.Application.Units.ListUnits;
using Recipes.Domain.Entities;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Units.CreateUnit;

public sealed class CreateUnitHandler : IRequestHandler<CreateUnitCommand, ErrorOr<MeasurementUnitDto>>
{
    private readonly IMeasurementUnitRepository _repository;

    public CreateUnitHandler(IMeasurementUnitRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<MeasurementUnitDto>> Handle(
        CreateUnitCommand request,
        CancellationToken cancellationToken)
    {
        var exists = await _repository.ExistsByAbbreviationAsync(request.Abbreviation, cancellationToken);
        if (exists)
            return Error.Conflict("Unit.DuplicateAbbreviation", $"A unit with abbreviation '{request.Abbreviation}' already exists.");

        var sortOrder = await _repository.GetNextSortOrderAsync(cancellationToken);
        var unit = new MeasurementUnit(request.Name, request.Abbreviation, sortOrder);
        await _repository.AddAsync(unit, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return new MeasurementUnitDto(unit.Id.Value, unit.Name, unit.Abbreviation);
    }
}
