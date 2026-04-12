using ErrorOr;
using MediatR;

namespace Recipes.Application.Households.AddPersonToHousehold;

public sealed record AddPersonToHouseholdCommand(Guid HouseholdId, Guid PersonId) : IRequest<ErrorOr<Success>>;