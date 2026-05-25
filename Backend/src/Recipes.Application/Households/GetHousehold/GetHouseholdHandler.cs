using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Households.GetHousehold;

public sealed class GetHouseholdHandler
    : IRequestHandler<GetHouseholdQuery, ErrorOr<HouseholdDetailsDto>>
{
    private readonly IHouseholdRepository _householdRepository;
    private readonly IPersonRepository _personRepository;
    private readonly ICurrentUser _currentUser;

    public GetHouseholdHandler(
        IHouseholdRepository householdRepository,
        IPersonRepository personRepository,
        ICurrentUser currentUser)
    {
        _householdRepository = householdRepository;
        _personRepository = personRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<HouseholdDetailsDto>> Handle(
        GetHouseholdQuery request,
        CancellationToken cancellationToken)
    {
        var householdId = HouseholdId.From(request.HouseholdId);
        var memberHouseholdIds = await _currentUser.GetHouseholdIdsAsync(cancellationToken);

        if (!memberHouseholdIds.Contains(householdId))
        {
            return Error.NotFound(
                "Household.NotFound",
                $"Household '{request.HouseholdId}' was not found.");
        }

        var household = await _householdRepository.GetByIdAsync(householdId, cancellationToken);

        if (household is null)
        {
            return Error.NotFound(
                "Household.NotFound",
                $"Household '{request.HouseholdId}' was not found.");
        }

        var persons = await _personRepository.GetAllAsync(cancellationToken);
        var personsById = persons.ToDictionary(x => x.Id, x => x);

        var members = household.People
            .Select(m =>
            {
                personsById.TryGetValue(m.PersonId, out var person);
                return (member: m, person);
            })
            .Where(x => x.person is not null)
            .Select(x => new HouseholdMemberDto(
                x.member.PersonId.Value,
                x.person!.Name,
                x.person.DietaryPreferences.Select(p => (int)p).ToList(),
                x.person.HealthConcerns.Select(p => (int)p).ToList(),
                x.person.Notes))
            .OrderBy(x => x.PersonName)
            .ToList();

        return new HouseholdDetailsDto(
            household.Id.Value,
            household.Name,
            members);
    }
}