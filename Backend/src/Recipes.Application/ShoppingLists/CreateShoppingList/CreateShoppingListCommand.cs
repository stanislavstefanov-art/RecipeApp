using ErrorOr;
using MediatR;

namespace Recipes.Application.ShoppingLists.CreateShoppingList;

public sealed record CreateShoppingListCommand(string Name) : IRequest<ErrorOr<CreateShoppingListResponse>>;