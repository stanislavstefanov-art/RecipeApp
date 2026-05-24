using ErrorOr;
using MediatR;

namespace Recipes.Application.ShoppingLists.AddManualItemToShoppingList;

public sealed record AddManualItemToShoppingListCommand(
    Guid ShoppingListId,
    string ProductName,
    decimal Quantity,
    string Unit) : IRequest<ErrorOr<Success>>;
