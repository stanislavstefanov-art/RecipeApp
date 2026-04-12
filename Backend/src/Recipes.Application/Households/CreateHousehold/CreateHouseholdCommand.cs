using ErrorOr;
using MediatR;

namespace Recipes.Application.Households.CreateHousehold;

public sealed record CreateHouseholdCommand(string Name) : IRequest<ErrorOr<CreateHouseholdResponse>>;