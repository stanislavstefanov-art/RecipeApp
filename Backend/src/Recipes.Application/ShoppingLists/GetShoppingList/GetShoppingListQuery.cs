using ErrorOr;
using MediatR;

namespace Recipes.Application.ShoppingLists.GetShoppingList;

public sealed record GetShoppingListQuery(Guid ShoppingListId) : IRequest<ErrorOr<ShoppingListDetailsDto>>;