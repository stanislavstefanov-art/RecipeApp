using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Persons.ListPersons;

public sealed class ListPersonsHandler
    : IRequestHandler<ListPersonsQuery, ErrorOr<IReadOnlyList<PersonDto>>>
{
    private readonly IPersonRepository _personRepository;
    private readonly ICurrentUser _currentUser;

    public ListPersonsHandler(IPersonRepository personRepository, ICurrentUser currentUser)
    {
        _personRepository = personRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<IReadOnlyList<PersonDto>>> Handle(
        ListPersonsQuery request,
        CancellationToken cancellationToken)
    {
        var householdIds = await _currentUser.GetHouseholdIdsAsync(cancellationToken);
        var persons = await _personRepository.GetByHouseholdIdsAsync(householdIds, cancellationToken);

        return persons.Select(x => new PersonDto(
            x.Id.Value,
            x.Name,
            x.DietaryPreferences.Select(y => (int)y).ToList(),
            x.HealthConcerns.Select(y => (int)y).ToList(),
            x.Notes)).ToList();
    }
}