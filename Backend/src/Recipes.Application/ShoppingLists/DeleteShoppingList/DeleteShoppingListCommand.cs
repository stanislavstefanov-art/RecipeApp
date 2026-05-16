using ErrorOr;
using MediatR;

namespace Recipes.Application.ShoppingLists.DeleteShoppingList;

public sealed record DeleteShoppingListCommand(Guid Id) : IRequest<ErrorOr<Deleted>>;
