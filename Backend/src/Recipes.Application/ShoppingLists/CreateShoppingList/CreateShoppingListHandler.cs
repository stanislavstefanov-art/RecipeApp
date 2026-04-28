using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.ShoppingLists.CreateShoppingList;

public sealed class CreateShoppingListHandler
    : IRequestHandler<CreateShoppingListCommand, ErrorOr<CreateShoppingListResponse>>
{
    private readonly IShoppingListRepository _shoppingListRepository;
    private readonly ICurrentUser _currentUser;

    public CreateShoppingListHandler(IShoppingListRepository shoppingListRepository, ICurrentUser currentUser)
    {
        _shoppingListRepository = shoppingListRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<CreateShoppingListResponse>> Handle(
        CreateShoppingListCommand request,
        CancellationToken cancellationToken)
    {
        var householdId = HouseholdId.From(request.HouseholdId);
        var memberIds = await _currentUser.GetHouseholdIdsAsync(cancellationToken);

        if (!memberIds.Contains(householdId))
        {
            return Error.Forbidden("ShoppingList.HouseholdAccessDenied", "You are not a member of the specified household.");
        }

        var shoppingList = new ShoppingList(request.Name, householdId);

        await _shoppingListRepository.AddAsync(shoppingList, cancellationToken);
        await _shoppingListRepository.SaveChangesAsync(cancellationToken);

        return new CreateShoppingListResponse(
            shoppingList.Id.Value,
            shoppingList.Name);
    }
}