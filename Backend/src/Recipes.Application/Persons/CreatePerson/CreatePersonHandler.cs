using ErrorOr;
using MediatR;
using Recipes.Domain.Entities;
using Recipes.Domain.Enums;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Persons.CreatePerson;

public sealed class CreatePersonHandler
    : IRequestHandler<CreatePersonCommand, ErrorOr<CreatePersonResponse>>
{
    private readonly IPersonRepository _personRepository;

    public CreatePersonHandler(IPersonRepository personRepository)
    {
        _personRepository = personRepository;
    }

    public async Task<ErrorOr<CreatePersonResponse>> Handle(
        CreatePersonCommand request,
        CancellationToken cancellationToken)
    {
        var person = new Person(
            request.Name,
            request.DietaryPreferences.Select(x => (DietaryPreference)x),
            request.HealthConcerns.Select(x => (HealthConcern)x),
            request.Notes);

        await _personRepository.AddAsync(person, cancellationToken);
        await _personRepository.SaveChangesAsync(cancellationToken);

        return new CreatePersonResponse(person.Id.Value, person.Name);
    }
}