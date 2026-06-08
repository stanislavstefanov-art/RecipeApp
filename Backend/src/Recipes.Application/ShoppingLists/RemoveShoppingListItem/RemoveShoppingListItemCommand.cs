using ErrorOr;
using MediatR;

namespace Recipes.Application.ShoppingLists.RemoveShoppingListItem;

public sealed record RemoveShoppingListItemCommand(Guid ShoppingListId, Guid ShoppingListItemId)
    : IRequest<ErrorOr<Success>>;
