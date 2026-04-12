using ErrorOr;
using MediatR;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Households.ListHouseholds;

public sealed class ListHouseholdsHandler
    : IRequestHandler<ListHouseholdsQuery, ErrorOr<IReadOnlyList<HouseholdListItemDto>>>
{
    private readonly IHouseholdRepository _householdRepository;

    public ListHouseholdsHandler(IHouseholdRepository householdRepository)
    {
        _householdRepository = householdRepository;
    }

    public async Task<ErrorOr<IReadOnlyList<HouseholdListItemDto>>> Handle(
        ListHouseholdsQuery request,
        CancellationToken cancellationToken)
    {
        var households = await _householdRepository.GetAllAsync(cancellationToken);

        return households
            .Select(x => new HouseholdListItemDto(
                x.Id.Value,
                x.Name,
                x.Members.Count))
            .ToList();
    }
}