using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Entities;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Households.CreateHousehold;

public sealed class CreateHouseholdHandler
    : IRequestHandler<CreateHouseholdCommand, ErrorOr<CreateHouseholdResponse>>
{
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentUser _currentUser;

    public CreateHouseholdHandler(IHouseholdRepository householdRepository, ICurrentUser currentUser)
    {
        _householdRepository = householdRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<CreateHouseholdResponse>> Handle(
        CreateHouseholdCommand request,
        CancellationToken cancellationToken)
    {
        var household = new Household(request.Name);
        household.AddUser(_currentUser.UserId, DateTimeOffset.UtcNow);

        await _householdRepository.AddAsync(household, cancellationToken);
        await _householdRepository.SaveChangesAsync(cancellationToken);

        _currentUser.InvalidateHouseholdCache();

        return new CreateHouseholdResponse(household.Id.Value, household.Name);
    }
}