using ErrorOr;
using MediatR;

namespace Recipes.Application.Households.ListHouseholds;

public sealed record ListHouseholdsQuery() : IRequest<ErrorOr<IReadOnlyList<HouseholdListItemDto>>>;