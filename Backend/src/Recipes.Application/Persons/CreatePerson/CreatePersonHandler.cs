using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Entities;
using Recipes.Domain.Enums;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Persons.CreatePerson;

public sealed class CreatePersonHandler
    : IRequestHandler<CreatePersonCommand, ErrorOr<CreatePersonResponse>>
{
    private readonly IPersonRepository _personRepository;
    private readonly ICurrentUser _currentUser;

    public CreatePersonHandler(IPersonRepository personRepository, ICurrentUser currentUser)
    {
        _personRepository = personRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<CreatePersonResponse>> Handle(
        CreatePersonCommand request,
        CancellationToken cancellationToken)
    {
        var householdId = HouseholdId.From(request.HouseholdId);
        var memberIds = await _currentUser.GetHouseholdIdsAsync(cancellationToken);

        if (!memberIds.Contains(householdId))
        {
            return Error.Forbidden("Person.HouseholdAccessDenied", "You are not a member of the specified household.");
        }

        var person = new Person(
            request.Name,
            request.DietaryPreferences.Select(x => (DietaryPreference)x),
            request.HealthConcerns.Select(x => (HealthConcern)x),
            request.Notes,
            householdId);

        await _personRepository.AddAsync(person, cancellationToken);
        await _personRepository.SaveChangesAsync(cancellationToken);

        return new CreatePersonResponse(person.Id.Value, person.Name);
    }
}