using ErrorOr;
using MediatR;

namespace Recipes.Application.ShoppingLists.AddItemToShoppingList;

public sealed record AddItemToShoppingListCommand(
    Guid ShoppingListId,
    Guid ProductId,
    decimal Quantity,
    string Unit) : IRequest<ErrorOr<Success>>;