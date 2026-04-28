using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Households.ListHouseholds;

public sealed class ListHouseholdsHandler
    : IRequestHandler<ListHouseholdsQuery, ErrorOr<IReadOnlyList<HouseholdListItemDto>>>
{
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentUser _currentUser;

    public ListHouseholdsHandler(IHouseholdRepository householdRepository, ICurrentUser currentUser)
    {
        _householdRepository = householdRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<IReadOnlyList<HouseholdListItemDto>>> Handle(
        ListHouseholdsQuery request,
        CancellationToken cancellationToken)
    {
        var households = await _householdRepository.GetByUserIdAsync(_currentUser.UserId, cancellationToken);

        return households
            .Select(x => new HouseholdListItemDto(
                x.Id.Value,
                x.Name,
                x.People.Count))
            .ToList();
    }
}