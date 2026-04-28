using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.ShoppingLists.MarkShoppingListItemPurchased;

public sealed class MarkShoppingListItemPurchasedHandler
    : IRequestHandler<MarkShoppingListItemPurchasedCommand, ErrorOr<Success>>
{
    private readonly IShoppingListRepository _shoppingListRepository;
    private readonly ICurrentUser _currentUser;

    public MarkShoppingListItemPurchasedHandler(IShoppingListRepository shoppingListRepository, ICurrentUser currentUser)
    {
        _shoppingListRepository = shoppingListRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<Success>> Handle(
        MarkShoppingListItemPurchasedCommand request,
        CancellationToken cancellationToken)
    {
        var shoppingListId = ShoppingListId.From(request.ShoppingListId);
        var itemId = ShoppingListItemId.From(request.ShoppingListItemId);

        var shoppingList = await _shoppingListRepository.GetByIdAsync(shoppingListId, cancellationToken);
        if (shoppingList is null)
        {
            return Error.NotFound(
                code: "ShoppingList.NotFound",
                description: $"Shopping list '{request.ShoppingListId}' was not found.");
        }

        if (shoppingList.HouseholdId.HasValue)
        {
            var memberIds = await _currentUser.GetHouseholdIdsAsync(cancellationToken);
            if (!memberIds.Contains(shoppingList.HouseholdId.Value))
            {
                return Error.NotFound(
                    code: "ShoppingList.NotFound",
                    description: $"Shopping list '{request.ShoppingListId}' was not found.");
            }
        }

        try
        {
            shoppingList.MarkItemPurchased(itemId);
        }
        catch (InvalidOperationException ex)
        {
            return Error.NotFound(
                code: "ShoppingListItem.NotFound",
                description: ex.Message);
        }

        await _shoppingListRepository.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}