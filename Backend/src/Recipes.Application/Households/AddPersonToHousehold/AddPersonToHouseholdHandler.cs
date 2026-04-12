using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Households.AddPersonToHousehold;

public sealed class AddPersonToHouseholdHandler
    : IRequestHandler<AddPersonToHouseholdCommand, ErrorOr<Success>>
{
    private readonly IHouseholdRepository _householdRepository;
    private readonly IPersonRepository _personRepository;

    public AddPersonToHouseholdHandler(
        IHouseholdRepository householdRepository,
        IPersonRepository personRepository)
    {
        _householdRepository = householdRepository;
        _personRepository = personRepository;
    }

    public async Task<ErrorOr<Success>> Handle(
        AddPersonToHouseholdCommand request,
        CancellationToken cancellationToken)
    {
        var household = await _householdRepository.GetByIdAsync(HouseholdId.From(request.HouseholdId), cancellationToken);
        if (household is null)
        {
            return Error.NotFound("Household.NotFound", $"Household '{request.HouseholdId}' was not found.");
        }

        var person = await _personRepository.GetByIdAsync(PersonId.From(request.PersonId), cancellationToken);
        if (person is null)
        {
            return Error.NotFound("Person.NotFound", $"Person '{request.PersonId}' was not found.");
        }

        household.AddMember(person);
        await _householdRepository.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}