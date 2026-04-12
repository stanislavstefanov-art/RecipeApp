using ErrorOr;
using MediatR;

namespace Recipes.Application.ShoppingLists.PurchaseShoppingListItem;

public sealed record PurchaseShoppingListItemCommand(
    Guid ShoppingListId,
    Guid ShoppingListItemId,
    decimal Amount,
    string Currency,
    DateOnly ExpenseDate,
    string? Description) : IRequest<ErrorOr<Success>>;