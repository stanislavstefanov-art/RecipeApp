using ErrorOr;
using MediatR;
using Recipes.Domain.Entities;
using Recipes.Domain.Enums;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.ShoppingLists.PurchaseShoppingListItem;

public sealed class PurchaseShoppingListItemHandler
    : IRequestHandler<PurchaseShoppingListItemCommand, ErrorOr<Success>>
{
    private readonly IShoppingListRepository _shoppingListRepository;
    private readonly IExpenseRepository _expenseRepository;

    public PurchaseShoppingListItemHandler(
        IShoppingListRepository shoppingListRepository,
        IExpenseRepository expenseRepository)
    {
        _shoppingListRepository = shoppingListRepository;
        _expenseRepository = expenseRepository;
    }

    public async Task<ErrorOr<Success>> Handle(
        PurchaseShoppingListItemCommand request,
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

        var item = shoppingList.Items.SingleOrDefault(x => x.Id == itemId);
        if (item is null)
        {
            return Error.NotFound(
                "ShoppingListItem.NotFound",
                $"Shopping list item '{request.ShoppingListItemId}' was not found.");
        }

        shoppingList.MarkItemPurchased(itemId);

        var description = string.IsNullOrWhiteSpace(request.Description)
            ? $"Purchased from shopping list: {item.ProductName}"
            : request.Description.Trim();

        var expense = new Expense(
            request.Amount,
            request.Currency,
            request.ExpenseDate,
            ExpenseCategory.Food,
            description,
            ExpenseSourceType.ShoppingList,
            item.Id.Value);

        await _expenseRepository.AddAsync(expense, cancellationToken);

        await _shoppingListRepository.SaveChangesAsync(cancellationToken);
        await _expenseRepository.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}