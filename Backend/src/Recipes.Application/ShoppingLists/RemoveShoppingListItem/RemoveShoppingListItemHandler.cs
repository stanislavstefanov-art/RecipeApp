using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.ShoppingLists.RemoveShoppingListItem;

public sealed class RemoveShoppingListItemHandler
    : IRequestHandler<RemoveShoppingListItemCommand, ErrorOr<Success>>
{
    private readonly IShoppingListRepository _shoppingListRepository;
    private readonly ICurrentUser _currentUser;

    public RemoveShoppingListItemHandler(IShoppingListRepository shoppingListRepository, ICurrentUser currentUser)
    {
        _shoppingListRepository = shoppingListRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<Success>> Handle(
        RemoveShoppingListItemCommand request,
        CancellationToken cancellationToken)
    {
        var shoppingListId = ShoppingListId.From(request.ShoppingListId);
        var itemId = ShoppingListItemId.From(request.ShoppingListItemId);

        var shoppingList = await _shoppingListRepository.GetByIdAsync(shoppingListId, cancellationToken);
        if (shoppingList is null)
        {
            return Error.NotFound(
                "ShoppingList.NotFound",
                $"Shopping list '{request.ShoppingListId}' was not found.");
        }

        if (shoppingList.HouseholdId.HasValue)
        {
            var memberIds = await _currentUser.GetHouseholdIdsAsync(cancellationToken);
            if (!memberIds.Contains(shoppingList.HouseholdId.Value))
            {
                return Error.NotFound(
                    "ShoppingList.NotFound",
                    $"Shopping list '{request.ShoppingListId}' was not found.");
            }
        }

        try
        {
            shoppingList.RemoveItem(itemId);
        }
        catch (InvalidOperationException ex)
        {
            return Error.NotFound(
                "ShoppingListItem.NotFound",
                ex.Message);
        }

        await _shoppingListRepository.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
