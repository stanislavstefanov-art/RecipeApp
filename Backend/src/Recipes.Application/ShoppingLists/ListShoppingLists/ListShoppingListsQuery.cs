using ErrorOr;
using MediatR;

namespace Recipes.Application.ShoppingLists.ListShoppingLists;

public sealed record ListShoppingListsQuery() : IRequest<ErrorOr<IReadOnlyList<ShoppingListDto>>>;