using ErrorOr;
using MediatR;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Persons.ListPersons;

public sealed class ListPersonsHandler
    : IRequestHandler<ListPersonsQuery, ErrorOr<IReadOnlyList<PersonDto>>>
{
    private readonly IPersonRepository _personRepository;

    public ListPersonsHandler(IPersonRepository personRepository)
    {
        _personRepository = personRepository;
    }

    public async Task<ErrorOr<IReadOnlyList<PersonDto>>> Handle(
        ListPersonsQuery request,
        CancellationToken cancellationToken)
    {
        var persons = await _personRepository.GetAllAsync(cancellationToken);

        return persons.Select(x => new PersonDto(
            x.Id.Value,
            x.Name,
            x.DietaryPreferences.Select(y => (int)y).ToList(),
            x.HealthConcerns.Select(y => (int)y).ToList(),
            x.Notes)).ToList();
    }
}