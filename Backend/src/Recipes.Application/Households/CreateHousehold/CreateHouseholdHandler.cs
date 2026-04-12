using ErrorOr;
using MediatR;
using Recipes.Domain.Entities;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Households.CreateHousehold;

public sealed class CreateHouseholdHandler
    : IRequestHandler<CreateHouseholdCommand, ErrorOr<CreateHouseholdResponse>>
{
    private readonly IHouseholdRepository _householdRepository;

    public CreateHouseholdHandler(IHouseholdRepository householdRepository)
    {
        _householdRepository = householdRepository;
    }

    public async Task<ErrorOr<CreateHouseholdResponse>> Handle(
        CreateHouseholdCommand request,
        CancellationToken cancellationToken)
    {
        var household = new Household(request.Name);

        await _householdRepository.AddAsync(household, cancellationToken);
        await _householdRepository.SaveChangesAsync(cancellationToken);

        return new CreateHouseholdResponse(household.Id.Value, household.Name);
    }
}