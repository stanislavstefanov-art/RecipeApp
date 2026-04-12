using ErrorOr;
using MediatR;

namespace Recipes.Application.Households.GetHousehold;

public sealed record GetHouseholdQuery(Guid HouseholdId) : IRequest<ErrorOr<HouseholdDetailsDto>>;