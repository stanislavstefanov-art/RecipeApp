using ErrorOr;
using MediatR;

namespace Recipes.Application.Units.ListUnits;

public sealed record ListUnitsQuery : IRequest<ErrorOr<IReadOnlyList<MeasurementUnitDto>>>;
