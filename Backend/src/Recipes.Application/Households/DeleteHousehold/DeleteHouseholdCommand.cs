using ErrorOr;
using MediatR;

namespace Recipes.Application.Households.DeleteHousehold;

public sealed record DeleteHouseholdCommand(Guid Id) : IRequest<ErrorOr<Deleted>>;
