using ErrorOr;
using MediatR;

namespace Recipes.Application.Auth.Me;

public sealed record MeQuery : IRequest<ErrorOr<MeDto>>;

public sealed record MeDto(
    AuthUserDto User,
    IReadOnlyList<HouseholdSummaryDto> Households);

public sealed record HouseholdSummaryDto(Guid Id, string Name);
