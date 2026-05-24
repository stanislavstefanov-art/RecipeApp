using ErrorOr;
using MediatR;
using Recipes.Application.Units.ListUnits;

namespace Recipes.Application.Units.CreateUnit;

public sealed record CreateUnitCommand(string Name, string Abbreviation) : IRequest<ErrorOr<MeasurementUnitDto>>;
