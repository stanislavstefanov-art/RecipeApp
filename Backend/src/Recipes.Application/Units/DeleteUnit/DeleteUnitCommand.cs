using ErrorOr;
using MediatR;

namespace Recipes.Application.Units.DeleteUnit;

public sealed record DeleteUnitCommand(Guid Id) : IRequest<ErrorOr<Deleted>>;
