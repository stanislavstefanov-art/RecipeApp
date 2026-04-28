using ErrorOr;
using MediatR;

namespace Recipes.Application.ShoppingLists.CreateShoppingList;

public sealed record CreateShoppingListCommand(string Name, Guid HouseholdId) : IRequest<ErrorOr<CreateShoppingListResponse>>;