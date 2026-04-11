using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.ShoppingLists.MarkShoppingListItemPending;

public sealed class MarkShoppingListItemPendingHandler
    : IRequestHandler<MarkShoppingListItemPendingCommand, ErrorOr<Success>>
{
    private readonly IShoppingListRepository _shoppingListRepository;

    public MarkShoppingListItemPendingHandler(IShoppingListRepository shoppingListRepository)
    {
        _shoppingListRepository = shoppingListRepository;
    }

    public async Task<ErrorOr<Success>> Handle(
        MarkShoppingListItemPendingCommand request,
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

        try
        {
            shoppingList.MarkItemPending(itemId);
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