using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Persons.GetPerson;

public sealed class GetPersonHandler
    : IRequestHandler<GetPersonQuery, ErrorOr<PersonDetailsDto>>
{
    private readonly IPersonRepository _personRepository;

    public GetPersonHandler(IPersonRepository personRepository)
    {
        _personRepository = personRepository;
    }

    public async Task<ErrorOr<PersonDetailsDto>> Handle(
        GetPersonQuery request,
        CancellationToken cancellationToken)
    {
        var person = await _personRepository.GetByIdAsync(
            PersonId.From(request.PersonId),
            cancellationToken);

        if (person is null)
        {
            return Error.NotFound(
                "Person.NotFound",
                $"Person '{request.PersonId}' was not found.");
        }

        return new PersonDetailsDto(
            person.Id.Value,
            person.Name,
            person.DietaryPreferences.Select(x => (int)x).ToList(),
            person.HealthConcerns.Select(x => (int)x).ToList(),
            person.Notes);
    }
}