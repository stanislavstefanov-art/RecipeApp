using ErrorOr;
using MediatR;

namespace Recipes.Application.ShoppingLists.MarkShoppingListItemPending;

public sealed record MarkShoppingListItemPendingCommand(
    Guid ShoppingListId,
    Guid ShoppingListItemId) : IRequest<ErrorOr<Success>>;