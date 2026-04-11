using ErrorOr;
using MediatR;

namespace Recipes.Application.ShoppingLists.MarkShoppingListItemPurchased;

public sealed record MarkShoppingListItemPurchasedCommand(
    Guid ShoppingListId,
    Guid ShoppingListItemId) : IRequest<ErrorOr<Success>>;